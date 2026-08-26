using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressImportantDates.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressImportantDates.Rules;

public class CongressImportantDateBusinessRules : BaseBusinessRules
{
    private static readonly string[] TranslationFieldNames =
    {
        "Title",
        "Description"
    };

    private readonly IApplicationLanguageProvider _applicationLanguageProvider;
    private readonly ICongressRepository _congressRepository;

    public CongressImportantDateBusinessRules(
        IApplicationLanguageProvider applicationLanguageProvider,
        ICongressRepository congressRepository)
    {
        _applicationLanguageProvider = applicationLanguageProvider;
        _congressRepository = congressRepository;
    }

    public async Task CongressShouldExist(Guid congressId, CancellationToken cancellationToken)
    {
        if (congressId == Guid.Empty)
            throw new BusinessException(CongressImportantDatesMessages.CongressRequired);

        Congress? congress = await _congressRepository.GetAsync(
            predicate: entity => entity.Id == congressId);

        if (congress is null)
            throw new BusinessException(CongressImportantDatesMessages.CongressNotFound);
    }

    public Task CongressImportantDateShouldExistWhenSelected(CongressImportantDate? entity)
    {
        if (entity is null)
            throw new BusinessException(CongressImportantDatesMessages.EntityNotFound);

        return Task.CompletedTask;
    }

    public Task ImportantDateShouldBelongToCongress(CongressImportantDate entity, Guid congressId)
    {
        if (congressId == Guid.Empty || entity.CongressId != congressId)
            throw new BusinessException(CongressImportantDatesMessages.EntityNotFound);

        return Task.CompletedTask;
    }

    public Task DateRangeShouldBeValid(DateTime startDate, DateTime endDate)
    {
        if (startDate == default)
            throw new BusinessException(CongressImportantDatesMessages.StartDateRequired);

        if (endDate == default)
            throw new BusinessException(CongressImportantDatesMessages.EndDateRequired);

        if (endDate < startDate)
            throw new BusinessException(CongressImportantDatesMessages.DateRangeInvalid);

        return Task.CompletedTask;
    }

    public Task OrderShouldBeValid(int order)
    {
        if (order < 0)
            throw new BusinessException(CongressImportantDatesMessages.InvalidOrder);

        return Task.CompletedTask;
    }

    public async Task DefaultTranslationShouldExist(
        IEnumerable<TranslationInputDto> translations,
        CancellationToken cancellationToken)
    {
        ApplicationLanguageDto defaultLanguage = await _applicationLanguageProvider.GetDefaultLanguageAsync(cancellationToken);

        TranslationInputDto? defaultTranslation = translations
            .FirstOrDefault(translation => translation.LanguageId == defaultLanguage.Id);

        if (defaultTranslation is null ||
            !LocalizedEntityRuntimeHelper.HasRequiredField(defaultTranslation.Fields, "Title"))
        {
            throw new BusinessException(CongressImportantDatesMessages.DefaultTranslationRequired);
        }
    }

    public async Task TranslationTitlesShouldBeValid(
        IEnumerable<TranslationInputDto> translations,
        CancellationToken cancellationToken)
    {
        ApplicationLanguageDto defaultLanguage = await _applicationLanguageProvider.GetDefaultLanguageAsync(cancellationToken);

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
                throw new BusinessException(CongressImportantDatesMessages.DefaultTranslationRequired);

            if (!isDefaultLanguage && hasAnyValue && !hasTitle)
                throw new BusinessException(CongressImportantDatesMessages.TranslationTitleRequired);
        }
    }

    public async Task DefaultTranslationCannotBeDeleted(
        Guid languageId,
        CancellationToken cancellationToken)
    {
        ApplicationLanguageDto defaultLanguage = await _applicationLanguageProvider.GetDefaultLanguageAsync(cancellationToken);

        if (languageId == defaultLanguage.Id)
            throw new BusinessException(CongressImportantDatesMessages.DefaultTranslationCannotBeDeleted);
    }

    public Task TranslationShouldExistWhenSelected(CongressImportantDateTranslation? translation)
    {
        if (translation is null)
            throw new BusinessException(CongressImportantDatesMessages.TranslationNotFound);

        return Task.CompletedTask;
    }

    public Task ReorderItemsShouldBeValid(IReadOnlyCollection<Guid> itemIds)
    {
        if (itemIds.Count == 0)
            throw new BusinessException(CongressImportantDatesMessages.ReorderRequired);

        if (itemIds.Any(id => id == Guid.Empty))
            throw new BusinessException(CongressImportantDatesMessages.InvalidReorderList);

        return Task.CompletedTask;
    }

    public Task ReorderItemsShouldBelongToCongress(
        IReadOnlyCollection<Guid> requestedIds,
        IReadOnlyDictionary<Guid, CongressImportantDate> entityById)
    {
        if (requestedIds.Any(id => !entityById.ContainsKey(id)))
            throw new BusinessException(CongressImportantDatesMessages.InvalidReorderList);

        return Task.CompletedTask;
    }
}
