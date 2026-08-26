using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.TransactionStatusPhases.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.TransactionStatusPhases.Rules;

public class TransactionStatusPhaseBusinessRules : BaseBusinessRules
{
    private const string NameField = "Name";

    private readonly IApplicationLanguageProvider _applicationLanguageProvider;
    private readonly ITransactionStatusPhaseRepository _phaseRepository;
    private readonly ITransactionStatusPhaseTranslationRepository _phaseTranslationRepository;
    private readonly ITransactionStatusRepository _transactionStatusRepository;

    public TransactionStatusPhaseBusinessRules(
        IApplicationLanguageProvider applicationLanguageProvider,
        ITransactionStatusPhaseRepository phaseRepository,
        ITransactionStatusPhaseTranslationRepository phaseTranslationRepository,
        ITransactionStatusRepository transactionStatusRepository)
    {
        _applicationLanguageProvider = applicationLanguageProvider;
        _phaseRepository = phaseRepository;
        _phaseTranslationRepository = phaseTranslationRepository;
        _transactionStatusRepository = transactionStatusRepository;
    }

    public Task TransactionStatusPhaseShouldExistWhenSelected(TransactionStatusPhase? entity)
    {
        if (entity is null)
            throw new BusinessException(TransactionStatusPhasesMessages.EntityNotFound);

        return Task.CompletedTask;
    }

    public Task TransactionStatusPhaseShouldBeActiveWhenSelected(TransactionStatusPhase? entity)
    {
        if (entity is null)
            throw new BusinessException(TransactionStatusPhasesMessages.EntityNotFound);

        if (!entity.IsActive)
            throw new BusinessException(TransactionStatusPhasesMessages.EntityNotFound);

        return Task.CompletedTask;
    }

    public async Task DefaultTranslationShouldExist(
        IEnumerable<TranslationInputDto> translations,
        CancellationToken cancellationToken)
    {
        ApplicationLanguageDto defaultLanguage = await _applicationLanguageProvider.GetDefaultLanguageAsync(cancellationToken);

        TranslationInputDto? defaultTranslation = translations
            .FirstOrDefault(translation => translation.LanguageId == defaultLanguage.Id);

        if (defaultTranslation is null || !LocalizedEntityRuntimeHelper.HasRequiredField(defaultTranslation.Fields, NameField))
            throw new BusinessException(TransactionStatusPhasesMessages.DefaultTranslationRequired);
    }

    public Task CodeShouldBeUniqueWhenCreating(string? code)
    {
        string normalizedCode = NormalizeCode(code);

        bool exists = _phaseRepository.Query()
            .ToList()
            .Where(entity => !IsDeleted(entity))
            .Any(entity => string.Equals(NormalizeCode(entity.Code), normalizedCode, StringComparison.Ordinal));

        if (exists)
            throw new BusinessException(TransactionStatusPhasesMessages.CodeAlreadyExists);

        return Task.CompletedTask;
    }

    public Task CodeShouldBeUniqueWhenUpdating(int id, string? code)
    {
        string normalizedCode = NormalizeCode(code);

        bool exists = _phaseRepository.Query()
            .ToList()
            .Where(entity => !IsDeleted(entity))
            .Any(entity => entity.Id != id && string.Equals(NormalizeCode(entity.Code), normalizedCode, StringComparison.Ordinal));

        if (exists)
            throw new BusinessException(TransactionStatusPhasesMessages.CodeAlreadyExists);

        return Task.CompletedTask;
    }

    public Task TransactionStatusPhaseShouldNotHaveTransactionStatuses(int phaseId)
    {
        bool isUsed = _transactionStatusRepository.Query()
            .ToList()
            .Where(entity => !IsDeleted(entity))
            .Any(entity => entity.TransactionStatusPhaseId == phaseId);

        if (isUsed)
            throw new BusinessException(TransactionStatusPhasesMessages.EntityUsedByTransactionStatus);

        return Task.CompletedTask;
    }

    public Task TranslationNamesShouldBeUniqueInRequest(IEnumerable<TranslationInputDto> translations)
    {
        bool hasDuplicate = translations
            .Where(translation => translation.LanguageId != Guid.Empty)
            .Select(translation => new
            {
                translation.LanguageId,
                Name = GetNormalizedName(translation)
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .GroupBy(item => new { item.LanguageId, item.Name })
            .Any(group => group.Count() > 1);

        if (hasDuplicate)
            throw new BusinessException(TransactionStatusPhasesMessages.DuplicateTranslationNameInRequest);

        return Task.CompletedTask;
    }

    public Task TranslationNamesShouldBeUniqueInDatabase(
        IEnumerable<TranslationInputDto> translations,
        int? excludedPhaseId)
    {
        List<TransactionStatusPhaseTranslation> existingTranslations = _phaseTranslationRepository
            .Query()
            .ToList()
            .Where(translation => !IsDeleted(translation))
            .ToList();

        foreach (TranslationInputDto input in translations)
        {
            string? normalizedName = GetNormalizedName(input);

            if (string.IsNullOrWhiteSpace(normalizedName))
                continue;

            bool exists = existingTranslations.Any(existing =>
                existing.LanguageId == input.LanguageId &&
                (!excludedPhaseId.HasValue || existing.TransactionStatusPhaseId != excludedPhaseId.Value) &&
                string.Equals(
                    Normalize(LocalizedEntityRuntimeHelper.GetPropertyValue(existing, NameField)?.ToString()),
                    normalizedName,
                    StringComparison.Ordinal));

            if (exists)
                throw new BusinessException(TransactionStatusPhasesMessages.NameAlreadyExists);
        }

        return Task.CompletedTask;
    }

    private static string? GetNormalizedName(TranslationInputDto translation)
    {
        if (!translation.Fields.TryGetValue(NameField, out string? name))
            return null;

        return Normalize(name);
    }

    private static string NormalizeCode(string? code)
    {
        return (code ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToLowerInvariant();
    }

    private static bool IsDeleted(object entity)
    {
        object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
        return deletedDate is not null;
    }
}
