using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.TransactionStatuses.Constants;
using Symplify.BackOffice.Application.Features.TransactionStatuses.Rules;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.TransactionStatuses.Commands.Create;

public class CreateTransactionStatusCommand
    : IRequest<CreatedTransactionStatusResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public int TransactionStatusPhaseId { get; set; }

    public string Code { get; set; } = string.Empty;

    public bool IsEditable { get; set; } = true;

    public bool IsFinal { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetTransactionStatuses";

    public string[] Roles => new[]
    {
        TransactionStatusesOperationClaims.Admin,
        TransactionStatusesOperationClaims.Write,
        TransactionStatusesOperationClaims.Add
    };

    public class CreateTransactionStatusCommandHandler
        : IRequestHandler<CreateTransactionStatusCommand, CreatedTransactionStatusResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name", "Description" };

        private readonly ITransactionStatusRepository _repository;
        private readonly ITransactionStatusTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly IMapper _mapper;
        private readonly TransactionStatusBusinessRules _rules;

        public CreateTransactionStatusCommandHandler(
            ITransactionStatusRepository repository,
            ITransactionStatusTranslationRepository translationRepository,
            IApplicationLanguageProvider languageProvider,
            IMapper mapper,
            TransactionStatusBusinessRules rules)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<CreatedTransactionStatusResponse> Handle(
            CreateTransactionStatusCommand request,
            CancellationToken cancellationToken)
        {
            await _rules.TransactionStatusPhaseShouldExistAndBeActive(request.TransactionStatusPhaseId);
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);
            await _rules.CodeShouldBeUniqueWhenCreating(request.Code);

            TransactionStatus entity = new()
            {
                TransactionStatusPhaseId = request.TransactionStatusPhaseId,
                Code = NormalizeCode(request.Code),
                Order = 0,
                IsEditable = request.IsEditable,
                IsFinal = request.IsFinal,
                IsActive = request.IsActive
            };

            TransactionStatus createdEntity = await _repository.AddAsync(entity);

            await NormalizePhaseOrdersAsync(createdEntity.TransactionStatusPhaseId, createdEntity, cancellationToken);
            await CreateTranslationsAsync(createdEntity.Id, request.Translations, cancellationToken);

            return _mapper.Map<CreatedTransactionStatusResponse>(createdEntity);
        }

        private async Task CreateTranslationsAsync(
            int rootId,
            IEnumerable<TranslationInputDto> translations,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            HashSet<Guid> activeLanguageIds = activeLanguages.Select(language => language.Id).ToHashSet();
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);

            foreach (TranslationInputDto input in translations)
            {
                if (!activeLanguageIds.Contains(input.LanguageId))
                    continue;

                bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);

                if (!isDefaultLanguage && !hasAnyValue)
                    continue;

                TransactionStatusTranslation translation = new();
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "TransactionStatusId", rootId);
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "LanguageId", input.LanguageId);
                LocalizedEntityRuntimeHelper.ApplyFieldDictionary(translation, TranslationFieldNames, input.Fields);

                await _translationRepository.AddAsync(translation);
            }
        }

        private async Task NormalizePhaseOrdersAsync(
            int phaseId,
            TransactionStatus createdEntity,
            CancellationToken cancellationToken)
        {
            List<TransactionStatus> entities = _repository.Query()
                .ToList()
                .Where(entity =>
                    !IsDeleted(entity) &&
                    entity.TransactionStatusPhaseId == phaseId &&
                    entity.Id != createdEntity.Id)
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.Id)
                .ToList();

            entities.Add(createdEntity);

            await PersistNormalizedOrdersAsync(entities, cancellationToken);
        }

        private async Task PersistNormalizedOrdersAsync(
            IReadOnlyList<TransactionStatus> entities,
            CancellationToken cancellationToken)
        {
            for (int index = 0; index < entities.Count; index++)
            {
                int normalizedOrder = index + 1;

                if (entities[index].Order == normalizedOrder)
                    continue;

                entities[index].Order = normalizedOrder;
                await _repository.UpdateAsync(entities[index]);
            }
        }

        private static bool IsDeleted(object entity)
        {
            object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
            return deletedDate is not null;
        }

        private static string NormalizeCode(string code)
        {
            return code.Trim().ToUpperInvariant();
        }
    }
}
