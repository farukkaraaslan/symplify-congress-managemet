using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.TransactionStatusPhases.Constants;
using Symplify.BackOffice.Application.Features.TransactionStatusPhases.Rules;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.TransactionStatusPhases.Commands.Create;

public class CreateTransactionStatusPhaseCommand
    : IRequest<CreatedTransactionStatusPhaseResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public string Code { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetTransactionStatusPhases";

    public string[] Roles => new[]
    {
        TransactionStatusPhasesOperationClaims.Admin,
        TransactionStatusPhasesOperationClaims.Write,
        TransactionStatusPhasesOperationClaims.Add
    };

    public class CreateTransactionStatusPhaseCommandHandler
        : IRequestHandler<CreateTransactionStatusPhaseCommand, CreatedTransactionStatusPhaseResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name", "Description" };

        private readonly ITransactionStatusPhaseRepository _repository;
        private readonly ITransactionStatusPhaseTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly IMapper _mapper;
        private readonly TransactionStatusPhaseBusinessRules _rules;

        public CreateTransactionStatusPhaseCommandHandler(
            ITransactionStatusPhaseRepository repository,
            ITransactionStatusPhaseTranslationRepository translationRepository,
            IApplicationLanguageProvider languageProvider,
            IMapper mapper,
            TransactionStatusPhaseBusinessRules rules)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<CreatedTransactionStatusPhaseResponse> Handle(
            CreateTransactionStatusPhaseCommand request,
            CancellationToken cancellationToken)
        {
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);
            await _rules.TranslationNamesShouldBeUniqueInRequest(request.Translations);
            await _rules.TranslationNamesShouldBeUniqueInDatabase(request.Translations, excludedPhaseId: null);
            await _rules.CodeShouldBeUniqueWhenCreating(request.Code);

            TransactionStatusPhase entity = new()
            {
                Code = NormalizeCode(request.Code),
                Order = 0,
                IsActive = request.IsActive
            };

            TransactionStatusPhase createdEntity = await _repository.AddAsync(entity);

            await NormalizeVisibleOrdersAsync(createdEntity, cancellationToken);
            await CreateTranslationsAsync(createdEntity.Id, request.Translations, cancellationToken);

            return _mapper.Map<CreatedTransactionStatusPhaseResponse>(createdEntity);
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

                TransactionStatusPhaseTranslation translation = new();
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "TransactionStatusPhaseId", rootId);
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "LanguageId", input.LanguageId);
                LocalizedEntityRuntimeHelper.ApplyFieldDictionary(translation, TranslationFieldNames, input.Fields);

                await _translationRepository.AddAsync(translation);
            }
        }

        private async Task NormalizeVisibleOrdersAsync(
            TransactionStatusPhase createdEntity,
            CancellationToken cancellationToken)
        {
            List<TransactionStatusPhase> entities = _repository.Query()
                .ToList()
                .Where(entity => !IsDeleted(entity) && entity.Id != createdEntity.Id)
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.Id)
                .ToList();

            entities.Add(createdEntity);

            await PersistNormalizedOrdersAsync(entities, cancellationToken);
        }

        private async Task PersistNormalizedOrdersAsync(
            IReadOnlyList<TransactionStatusPhase> entities,
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

        private static string NormalizeCode(string code)
        {
            return code.Trim().ToUpperInvariant();
        }

        private static bool IsDeleted(object entity)
        {
            object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
            return deletedDate is not null;
        }
    }
}
