using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressAnnouncements.Commands.Reorder;
using Symplify.BackOffice.Application.Features.CongressAnnouncements.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressAnnouncements.Rules;

public class CongressAnnouncementBusinessRules : BaseBusinessRules
{
    private const int ExternalUrlMaxLength = 1000;
    private const int AttachmentPathMaxLength = 1000;

    private static readonly string[] TranslationFieldNames =
    {
        "Title",
        "Summary",
        "Content",
        "SeoTitle",
        "SeoDescription"
    };

    private readonly IApplicationLanguageProvider _languageProvider;
    private readonly ICongressRepository _congressRepository;
    private readonly ICongressAnnouncementRepository _announcementRepository;

    public CongressAnnouncementBusinessRules(
        IApplicationLanguageProvider languageProvider,
        ICongressRepository congressRepository,
        ICongressAnnouncementRepository announcementRepository)
    {
        _languageProvider = languageProvider;
        _congressRepository = congressRepository;
        _announcementRepository = announcementRepository;
    }

    public async Task CongressShouldExist(Guid congressId, CancellationToken cancellationToken)
    {
        if (congressId == Guid.Empty)
            throw new BusinessException(CongressAnnouncementsMessages.CongressRequired);

        Congress? congress = await _congressRepository.GetAsync(predicate: item => item.Id == congressId);

        if (congress is null)
            throw new BusinessException(CongressAnnouncementsMessages.CongressNotFound);
    }

    public Task AnnouncementShouldExistWhenSelected(CongressAnnouncement? entity)
    {
        if (entity is null)
            throw new BusinessException(CongressAnnouncementsMessages.EntityNotFound);

        return Task.CompletedTask;
    }

    public Task AnnouncementShouldBelongToCongress(CongressAnnouncement entity, Guid congressId)
    {
        if (congressId == Guid.Empty || entity.CongressId != congressId)
            throw new BusinessException(CongressAnnouncementsMessages.EntityNotFound);

        return Task.CompletedTask;
    }

    public Task PublishDateRangeShouldBeValid(DateTime? publishStartDate, DateTime? publishEndDate)
    {
        if (publishStartDate.HasValue && publishEndDate.HasValue && publishEndDate.Value < publishStartDate.Value)
            throw new BusinessException(CongressAnnouncementsMessages.PublishDateRangeInvalid);

        return Task.CompletedTask;
    }

    public Task OrderShouldBeValid(int order)
    {
        if (order < 0)
            throw new BusinessException(CongressAnnouncementsMessages.OrderInvalid);

        return Task.CompletedTask;
    }

    public Task ExternalUrlShouldBeValid(string? externalUrl)
    {
        if (!string.IsNullOrWhiteSpace(externalUrl) && externalUrl.Trim().Length > ExternalUrlMaxLength)
            throw new BusinessException(CongressAnnouncementsMessages.ExternalUrlTooLong);

        return Task.CompletedTask;
    }

    public Task AttachmentPathShouldBeValid(string? attachmentPath)
    {
        if (!string.IsNullOrWhiteSpace(attachmentPath) && attachmentPath.Trim().Length > AttachmentPathMaxLength)
            throw new BusinessException(CongressAnnouncementsMessages.AttachmentPathTooLong);

        return Task.CompletedTask;
    }

    public async Task DefaultTranslationShouldExist(
        IEnumerable<TranslationInputDto> translations,
        CancellationToken cancellationToken)
    {
        ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);

        TranslationInputDto? defaultTranslation = translations
            .FirstOrDefault(translation => translation.LanguageId == defaultLanguage.Id);

        if (defaultTranslation is null ||
            !LocalizedEntityRuntimeHelper.HasRequiredField(defaultTranslation.Fields, "Title"))
        {
            throw new BusinessException(CongressAnnouncementsMessages.TitleRequired);
        }
    }

    public async Task TranslationTitlesShouldBeValid(
        IEnumerable<TranslationInputDto> translations,
        CancellationToken cancellationToken)
    {
        ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);

        foreach (TranslationInputDto translation in translations)
        {
            bool isDefaultLanguage = translation.LanguageId == defaultLanguage.Id;
            bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(translation.Fields, TranslationFieldNames);
            bool hasTitle = LocalizedEntityRuntimeHelper.HasRequiredField(translation.Fields, "Title");

            if (isDefaultLanguage && !hasTitle)
                throw new BusinessException(CongressAnnouncementsMessages.TitleRequired);

            if (!isDefaultLanguage && hasAnyValue && !hasTitle)
                throw new BusinessException(CongressAnnouncementsMessages.TranslationTitleRequired);
        }
    }

    public async Task DefaultTranslationCannotBeDeleted(
        Guid languageId,
        CancellationToken cancellationToken)
    {
        ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);

        if (languageId == defaultLanguage.Id)
            throw new BusinessException(CongressAnnouncementsMessages.DefaultTranslationCannotBeDeleted);
    }

    public Task TranslationShouldExistWhenSelected(CongressAnnouncementTranslation? translation)
    {
        if (translation is null)
            throw new BusinessException(CongressAnnouncementsMessages.TranslationNotFound);

        return Task.CompletedTask;
    }

    public Task ReorderItemsShouldBeValid(IReadOnlyCollection<ReorderCongressAnnouncementItemDto> items)
    {
        if (items.Count == 0)
            throw new BusinessException(CongressAnnouncementsMessages.ReorderRequired);

        if (items.Any(item => item.Id == Guid.Empty))
            throw new BusinessException(CongressAnnouncementsMessages.InvalidReorderList);

        return Task.CompletedTask;
    }

    public Task ReorderItemsShouldBelongToCongress(
        IReadOnlyCollection<ReorderCongressAnnouncementItemDto> requestedItems,
        IReadOnlyDictionary<Guid, CongressAnnouncement> entityById)
    {
        if (requestedItems.Any(item => !entityById.ContainsKey(item.Id)))
            throw new BusinessException(CongressAnnouncementsMessages.InvalidReorderList);

        return Task.CompletedTask;
    }
}
