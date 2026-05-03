using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Constants;
using Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Rules;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Commands.Create;

public class CreateTransactionStatusTransitionCommand
    : IRequest<CreatedTransactionStatusTransitionResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public int FromStatusId { get; set; }

    public int ToStatusId { get; set; }

    public bool IsActive { get; set; }

    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetTransactionStatusTransitions";

    public string[] Roles => new[]
    {
        TransactionStatusTransitionsOperationClaims.Admin,
        TransactionStatusTransitionsOperationClaims.Write,
        TransactionStatusTransitionsOperationClaims.Add
    };

    public class CreateTransactionStatusTransitionCommandHandler
        : IRequestHandler<CreateTransactionStatusTransitionCommand, CreatedTransactionStatusTransitionResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name", "Description" };

        private readonly ITransactionStatusTransitionRepository _repository;
        private readonly ITransactionStatusTransitionTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly IMapper _mapper;
        private readonly TransactionStatusTransitionBusinessRules _rules;

        public CreateTransactionStatusTransitionCommandHandler(
            ITransactionStatusTransitionRepository repository,
            ITransactionStatusTransitionTranslationRepository translationRepository,
            IApplicationLanguageProvider languageProvider,
            IMapper mapper,
            TransactionStatusTransitionBusinessRules rules)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<CreatedTransactionStatusTransitionResponse> Handle(
            CreateTransactionStatusTransitionCommand request,
            CancellationToken cancellationToken)
        {
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);
            await _rules.FromAndToStatusShouldBeDifferent(request.FromStatusId, request.ToStatusId);
            await _rules.StatusesShouldExistAndBeUsable(request.FromStatusId, request.ToStatusId);
            await _rules.TransitionShouldBeUniqueWhenCreating(request.FromStatusId, request.ToStatusId);

            TransactionStatusTransition entity = new()
            {
                FromStatusId = request.FromStatusId,
                ToStatusId = request.ToStatusId,
                IsActive = request.IsActive
            };

            TransactionStatusTransition createdEntity = await _repository.AddAsync(entity);

            await CreateTranslationsAsync(createdEntity.Id, request.Translations, cancellationToken);

            return _mapper.Map<CreatedTransactionStatusTransitionResponse>(createdEntity);
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

                TransactionStatusTransitionTranslation translation = new();
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "TransactionStatusTransitionId", rootId);
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "LanguageId", input.LanguageId);
                LocalizedEntityRuntimeHelper.ApplyFieldDictionary(translation, TranslationFieldNames, input.Fields);

                await _translationRepository.AddAsync(translation);
            }
        }
    }
}
