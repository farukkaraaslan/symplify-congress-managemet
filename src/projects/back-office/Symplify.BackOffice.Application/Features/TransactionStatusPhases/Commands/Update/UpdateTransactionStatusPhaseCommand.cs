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

namespace Symplify.BackOffice.Application.Features.TransactionStatusPhases.Commands.Update;

public class UpdateTransactionStatusPhaseCommand
    : IRequest<UpdatedTransactionStatusPhaseResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public int Id { get; set; }

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
        TransactionStatusPhasesOperationClaims.Update
    };

    public class UpdateTransactionStatusPhaseCommandHandler
        : IRequestHandler<UpdateTransactionStatusPhaseCommand, UpdatedTransactionStatusPhaseResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name", "Description" };

        private readonly ITransactionStatusPhaseRepository _repository;
        private readonly ITransactionStatusPhaseTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly IMapper _mapper;
        private readonly TransactionStatusPhaseBusinessRules _rules;

        public UpdateTransactionStatusPhaseCommandHandler(
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

        public async Task<UpdatedTransactionStatusPhaseResponse> Handle(
            UpdateTransactionStatusPhaseCommand request,
            CancellationToken cancellationToken)
        {
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);
            await _rules.TranslationNamesShouldBeUniqueInRequest(request.Translations);
            await _rules.TranslationNamesShouldBeUniqueInDatabase(request.Translations, request.Id);
            await _rules.CodeShouldBeUniqueWhenUpdating(request.Id, request.Code);

            TransactionStatusPhase? entity = await _repository.GetAsync(predicate: root => root.Id.Equals(request.Id));
            await _rules.TransactionStatusPhaseShouldExistWhenSelected(entity);

            entity!.Code = NormalizeCode(request.Code);
            entity.IsActive = request.IsActive;

            TransactionStatusPhase updatedEntity = await _repository.UpdateAsync(entity);

            await UpsertTranslationsAsync(updatedEntity.Id, request.Translations, cancellationToken);

            return _mapper.Map<UpdatedTransactionStatusPhaseResponse>(updatedEntity);
        }

        private async Task UpsertTranslationsAsync(
            int rootId,
            IEnumerable<TranslationInputDto> translations,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            HashSet<Guid> activeLanguageIds = activeLanguages.Select(language => language.Id).ToHashSet();
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);

            List<TransactionStatusPhaseTranslation> existingTranslations = _translationRepository.Query()
                .ToList()
                .Where(translation => translation.TransactionStatusPhaseId == rootId)
                .ToList();

            foreach (TranslationInputDto input in translations)
            {
                if (!activeLanguageIds.Contains(input.LanguageId))
                    continue;

                bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);

                if (!isDefaultLanguage && !hasAnyValue)
                    continue;

                TransactionStatusPhaseTranslation? existingTranslation = existingTranslations
                    .FirstOrDefault(translation => translation.LanguageId == input.LanguageId);

                if (existingTranslation is null)
                {
                    TransactionStatusPhaseTranslation translation = new();
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "TransactionStatusPhaseId", rootId);
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "LanguageId", input.LanguageId);
                    LocalizedEntityRuntimeHelper.ApplyFieldDictionary(translation, TranslationFieldNames, input.Fields);

                    await _translationRepository.AddAsync(translation);
                    continue;
                }

                LocalizedEntityRuntimeHelper.ApplyFieldDictionary(existingTranslation, TranslationFieldNames, input.Fields);
                await _translationRepository.UpdateAsync(existingTranslation);
            }
        }

        private static string NormalizeCode(string code)
        {
            return code.Trim().ToUpperInvariant();
        }
    }
}
