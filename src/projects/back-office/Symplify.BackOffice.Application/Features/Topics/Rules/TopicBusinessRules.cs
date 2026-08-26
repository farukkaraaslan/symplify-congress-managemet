using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.Topics.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Application.Features.Topics.Rules;

public class TopicBusinessRules : BaseBusinessRules
{
    private const string NameField = "Name";

    private readonly IApplicationLanguageProvider _applicationLanguageProvider;
    private readonly ITopicTranslationRepository _topicTranslationRepository;

    public TopicBusinessRules(
        IApplicationLanguageProvider applicationLanguageProvider,
        ITopicTranslationRepository topicTranslationRepository)
    {
        _applicationLanguageProvider = applicationLanguageProvider;
        _topicTranslationRepository = topicTranslationRepository;
    }

    public Task TopicShouldExistWhenSelected(Topic? entity)
    {
        if (entity is null)
            throw new BusinessException(TopicsMessages.EntityNotFound);

        return Task.CompletedTask;
    }

    public async Task DefaultTranslationShouldExist(
        IEnumerable<TranslationInputDto> translations,
        CancellationToken cancellationToken)
    {
        ApplicationLanguageDto defaultLanguage = await _applicationLanguageProvider
            .GetDefaultLanguageAsync(cancellationToken);

        TranslationInputDto? defaultTranslation = translations
            .FirstOrDefault(translation => translation.LanguageId == defaultLanguage.Id);

        if (defaultTranslation is null || !LocalizedEntityRuntimeHelper.HasRequiredField(defaultTranslation.Fields, NameField))
            throw new BusinessException(TopicsMessages.DefaultTranslationRequired);
    }

    public Task TranslationNamesShouldBeUniqueInRequest(
        IEnumerable<TranslationInputDto> translations)
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
            throw new BusinessException(TopicsMessages.DuplicateTranslationNameInRequest);

        return Task.CompletedTask;
    }

    public Task TranslationNamesShouldBeUniqueInDatabase(
        IEnumerable<TranslationInputDto> translations,
        Guid? excludedTopicId)
    {
        List<TopicTranslation> existingTranslations = _topicTranslationRepository
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
                (!excludedTopicId.HasValue || existing.TopicId != excludedTopicId.Value) &&
                string.Equals(
                    Normalize(LocalizedEntityRuntimeHelper.GetPropertyValue(existing, NameField)?.ToString()),
                    normalizedName,
                    StringComparison.Ordinal));

            if (exists)
                throw new BusinessException(TopicsMessages.NameAlreadyExists);
        }

        return Task.CompletedTask;
    }

    private static string? GetNormalizedName(TranslationInputDto translation)
    {
        if (!translation.Fields.TryGetValue(NameField, out string? name))
            return null;

        return Normalize(name);
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
