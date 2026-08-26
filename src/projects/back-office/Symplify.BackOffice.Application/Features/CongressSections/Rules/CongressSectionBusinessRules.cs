using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressSections.Commands.Reorder;
using Symplify.BackOffice.Application.Features.CongressSections.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressSections.Rules;

public class CongressSectionBusinessRules : BaseBusinessRules
{
    private const int BindingKeyMaxLength = 100;

    private static readonly string[] TranslationFieldNames =
    {
        "Title",
        "Content"
    };

    private readonly IApplicationLanguageProvider _applicationLanguageProvider;
    private readonly ICongressRepository _congressRepository;
    private readonly ICongressSectionRepository _congressSectionRepository;

    public CongressSectionBusinessRules(
        IApplicationLanguageProvider applicationLanguageProvider,
        ICongressRepository congressRepository,
        ICongressSectionRepository congressSectionRepository)
    {
        _applicationLanguageProvider = applicationLanguageProvider;
        _congressRepository = congressRepository;
        _congressSectionRepository = congressSectionRepository;
    }

    public async Task CongressShouldExist(Guid congressId, CancellationToken cancellationToken)
    {
        if (congressId == Guid.Empty)
            throw new BusinessException(CongressSectionsMessages.CongressRequired);

        Congress? congress = await _congressRepository.GetAsync(
            predicate: entity => entity.Id == congressId);

        if (congress is null)
            throw new BusinessException(CongressSectionsMessages.CongressNotFound);
    }

    public Task CongressSectionShouldExistWhenSelected(CongressSection? entity)
    {
        if (entity is null)
            throw new BusinessException(CongressSectionsMessages.EntityNotFound);

        return Task.CompletedTask;
    }

    public Task SectionShouldBelongToCongress(CongressSection entity, Guid congressId)
    {
        if (congressId == Guid.Empty || entity.CongressId != congressId)
            throw new BusinessException(CongressSectionsMessages.EntityNotFound);

        return Task.CompletedTask;
    }

    public Task BindingKeyShouldBeValid(string? bindingKey)
    {
        if (string.IsNullOrWhiteSpace(bindingKey))
            throw new BusinessException(CongressSectionsMessages.BindingKeyRequired);

        if (bindingKey.Trim().Length > BindingKeyMaxLength)
            throw new BusinessException(CongressSectionsMessages.BindingKeyTooLong);

        return Task.CompletedTask;
    }

    public Task BindingKeyShouldBeUniqueForCongress(
        Guid congressId,
        string bindingKey,
        Guid? excludedSectionId = null)
    {
        string normalizedBindingKey = NormalizeBindingKey(bindingKey);

        bool exists = _congressSectionRepository
            .Query()
            .ToList()
            .Any(entity =>
                entity.CongressId == congressId &&
                (!excludedSectionId.HasValue || entity.Id != excludedSectionId.Value) &&
                !IsDeleted(entity) &&
                string.Equals(
                    NormalizeBindingKey(entity.BindingKey),
                    normalizedBindingKey,
                    StringComparison.OrdinalIgnoreCase));

        if (exists)
            throw new BusinessException(CongressSectionsMessages.BindingKeyAlreadyExists);

        return Task.CompletedTask;
    }

    public Task OrderShouldBeValid(int order)
    {
        if (order < 0)
            throw new BusinessException(CongressSectionsMessages.InvalidOrder);

        return Task.CompletedTask;
    }

    public async Task DefaultTranslationShouldExist(
        IEnumerable<TranslationInputDto> translations,
        CancellationToken cancellationToken)
    {
        ApplicationLanguageDto defaultLanguage =
            await _applicationLanguageProvider.GetDefaultLanguageAsync(cancellationToken);

        TranslationInputDto? defaultTranslation = translations
            .FirstOrDefault(translation => translation.LanguageId == defaultLanguage.Id);

        if (defaultTranslation is null ||
            !LocalizedEntityRuntimeHelper.HasRequiredField(defaultTranslation.Fields, "Title"))
        {
            throw new BusinessException(CongressSectionsMessages.DefaultTranslationRequired);
        }
    }

    public async Task TranslationTitlesShouldBeValid(
        IEnumerable<TranslationInputDto> translations,
        CancellationToken cancellationToken)
    {
        ApplicationLanguageDto defaultLanguage =
            await _applicationLanguageProvider.GetDefaultLanguageAsync(cancellationToken);

        foreach (TranslationInputDto translation in translations)
        {
            bool isDefaultLanguage = translation.LanguageId == defaultLanguage.Id;
            bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(
                translation.Fields,
                TranslationFieldNames);

            bool hasTitle = LocalizedEntityRuntimeHelper.HasRequiredField(
                translation.Fields,
                "Title");

            if (isDefaultLanguage && !hasTitle)
                throw new BusinessException(CongressSectionsMessages.DefaultTranslationRequired);

            if (!isDefaultLanguage && hasAnyValue && !hasTitle)
                throw new BusinessException(CongressSectionsMessages.TranslationTitleRequired);
        }
    }

    public async Task DefaultTranslationCannotBeDeleted(
        Guid languageId,
        CancellationToken cancellationToken)
    {
        ApplicationLanguageDto defaultLanguage =
            await _applicationLanguageProvider.GetDefaultLanguageAsync(cancellationToken);

        if (languageId == defaultLanguage.Id)
            throw new BusinessException(CongressSectionsMessages.DefaultTranslationCannotBeDeleted);
    }

    public Task TranslationShouldExistWhenSelected(CongressSectionTranslation? translation)
    {
        if (translation is null)
            throw new BusinessException(CongressSectionsMessages.TranslationNotFound);

        return Task.CompletedTask;
    }

    public Task ReorderItemsShouldBeValid(IReadOnlyCollection<ReorderCongressSectionItemDto> items)
    {
        if (items.Count == 0)
            throw new BusinessException(CongressSectionsMessages.ReorderRequired);

        if (items.Any(item => item.Id == Guid.Empty))
            throw new BusinessException(CongressSectionsMessages.InvalidReorderList);

        return Task.CompletedTask;
    }

    public Task ReorderItemsShouldBelongToCongress(
        IReadOnlyCollection<ReorderCongressSectionItemDto> requestedItems,
        IReadOnlyDictionary<Guid, CongressSection> entityById)
    {
        if (requestedItems.Any(item => !entityById.ContainsKey(item.Id)))
            throw new BusinessException(CongressSectionsMessages.InvalidReorderList);

        return Task.CompletedTask;
    }

    private static string NormalizeBindingKey(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();
    }

    private static bool IsDeleted(object entity)
    {
        object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
        return deletedDate is not null;
    }
}
