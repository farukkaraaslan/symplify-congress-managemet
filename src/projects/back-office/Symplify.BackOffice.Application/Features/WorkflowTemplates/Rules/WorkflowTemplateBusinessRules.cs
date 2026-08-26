using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.WorkflowTemplates.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.WorkflowTemplates.Rules;

public class WorkflowTemplateBusinessRules : BaseBusinessRules
{
    private const string NameField = "Name";

    private readonly IApplicationLanguageProvider _applicationLanguageProvider;
    private readonly IWorkflowTemplateRepository _templateRepository;
    private readonly IWorkflowTemplateTranslationRepository _translationRepository;
    private readonly ITransactionStatusRepository _transactionStatusRepository;
    private readonly ICongressWorkflowSettingRepository _congressWorkflowSettingRepository;

    public WorkflowTemplateBusinessRules(
        IApplicationLanguageProvider applicationLanguageProvider,
        IWorkflowTemplateRepository templateRepository,
        IWorkflowTemplateTranslationRepository translationRepository,
        ITransactionStatusRepository transactionStatusRepository,
        ICongressWorkflowSettingRepository congressWorkflowSettingRepository)
    {
        _applicationLanguageProvider = applicationLanguageProvider;
        _templateRepository = templateRepository;
        _translationRepository = translationRepository;
        _transactionStatusRepository = transactionStatusRepository;
        _congressWorkflowSettingRepository = congressWorkflowSettingRepository;
    }

    public Task WorkflowTemplateShouldExistWhenSelected(WorkflowTemplate? entity)
    {
        if (entity is null)
            throw new BusinessException(WorkflowTemplatesMessages.EntityNotFound);

        return Task.CompletedTask;
    }

    public async Task DefaultTranslationShouldExist(IEnumerable<TranslationInputDto> translations, CancellationToken cancellationToken)
    {
        ApplicationLanguageDto defaultLanguage = await _applicationLanguageProvider.GetDefaultLanguageAsync(cancellationToken);
        TranslationInputDto? defaultTranslation = translations.FirstOrDefault(translation => translation.LanguageId == defaultLanguage.Id);

        if (defaultTranslation is null || !LocalizedEntityRuntimeHelper.HasRequiredField(defaultTranslation.Fields, NameField))
            throw new BusinessException(WorkflowTemplatesMessages.DefaultTranslationRequired);
    }

    public Task CodeShouldBeUniqueWhenCreating(string? code)
    {
        string normalizedCode = NormalizeCode(code);
        bool exists = _templateRepository.Query()
            .ToList()
            .Where(entity => !IsDeleted(entity))
            .Any(entity => string.Equals(NormalizeCode(entity.Code), normalizedCode, StringComparison.Ordinal));

        if (exists)
            throw new BusinessException(WorkflowTemplatesMessages.CodeAlreadyExists);

        return Task.CompletedTask;
    }

    public Task CodeShouldBeUniqueWhenUpdating(Guid id, string? code)
    {
        string normalizedCode = NormalizeCode(code);
        bool exists = _templateRepository.Query()
            .ToList()
            .Where(entity => !IsDeleted(entity))
            .Any(entity => entity.Id != id && string.Equals(NormalizeCode(entity.Code), normalizedCode, StringComparison.Ordinal));

        if (exists)
            throw new BusinessException(WorkflowTemplatesMessages.CodeAlreadyExists);

        return Task.CompletedTask;
    }

    public Task InitialStatusShouldBeValid(int? initialTransactionStatusId)
    {
        if (!initialTransactionStatusId.HasValue)
            return Task.CompletedTask;

        TransactionStatus? status = _transactionStatusRepository.Query()
            .ToList()
            .Where(entity => !IsDeleted(entity))
            .FirstOrDefault(entity => entity.Id == initialTransactionStatusId.Value);

        if (status is null)
            throw new BusinessException(WorkflowTemplatesMessages.InitialStatusNotFound);

        if (!status.IsActive)
            throw new BusinessException(WorkflowTemplatesMessages.InitialStatusInactive);

        if (status.IsFinal)
            throw new BusinessException(WorkflowTemplatesMessages.InitialStatusIsFinal);

        return Task.CompletedTask;
    }

    public Task WorkflowTemplateShouldNotBeUsedByCongress(Guid workflowTemplateId)
    {
        bool isUsed = _congressWorkflowSettingRepository.Query()
            .ToList()
            .Where(entity => !IsDeleted(entity))
            .Any(entity => entity.SourceWorkflowTemplateId == workflowTemplateId);

        if (isUsed)
            throw new BusinessException(WorkflowTemplatesMessages.EntityUsedByCongressWorkflow);

        return Task.CompletedTask;
    }

    public Task TranslationNamesShouldBeUniqueInRequest(IEnumerable<TranslationInputDto> translations)
    {
        bool hasDuplicate = translations
            .Where(translation => translation.LanguageId != Guid.Empty)
            .Select(translation => new { translation.LanguageId, Name = GetNormalizedName(translation) })
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .GroupBy(item => new { item.LanguageId, item.Name })
            .Any(group => group.Count() > 1);

        if (hasDuplicate)
            throw new BusinessException(WorkflowTemplatesMessages.DuplicateTranslationNameInRequest);

        return Task.CompletedTask;
    }

    public Task TranslationNamesShouldBeUniqueInDatabase(IEnumerable<TranslationInputDto> translations, Guid? excludedWorkflowTemplateId)
    {
        List<WorkflowTemplateTranslation> existingTranslations = _translationRepository.Query().ToList().Where(translation => !IsDeleted(translation)).ToList();

        foreach (TranslationInputDto input in translations)
        {
            string? normalizedName = GetNormalizedName(input);
            if (string.IsNullOrWhiteSpace(normalizedName))
                continue;

            bool exists = existingTranslations.Any(existing =>
                existing.LanguageId == input.LanguageId &&
                (!excludedWorkflowTemplateId.HasValue || existing.WorkflowTemplateId != excludedWorkflowTemplateId.Value) &&
                string.Equals(Normalize(LocalizedEntityRuntimeHelper.GetPropertyValue(existing, NameField)?.ToString()), normalizedName, StringComparison.Ordinal));

            if (exists)
                throw new BusinessException(WorkflowTemplatesMessages.NameAlreadyExists);
        }

        return Task.CompletedTask;
    }

    private static string? GetNormalizedName(TranslationInputDto translation)
    {
        if (!translation.Fields.TryGetValue(NameField, out string? name))
            return null;

        return Normalize(name);
    }

    private static string NormalizeCode(string? code) => (code ?? string.Empty).Trim().ToUpperInvariant();
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    private static bool IsDeleted(object entity)
    {
        object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
        return deletedDate is not null;
    }
}
