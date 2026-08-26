using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Common.Storage;
using Symplify.BackOffice.Application.Features.CongressSliders.Commands;
using Symplify.BackOffice.Application.Features.CongressSliders.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressSliders.Rules;

public class CongressSliderBusinessRules : BaseBusinessRules
{
    private readonly ICongressRepository _congressRepository;

    public CongressSliderBusinessRules(ICongressRepository congressRepository)
    {
        _congressRepository = congressRepository;
    }

    public async Task CongressShouldExist(Guid congressId, CancellationToken cancellationToken)
    {
        if (congressId == Guid.Empty)
            throw new BusinessException(CongressSlidersMessages.CongressNotFound);

        Congress? congress = await _congressRepository.GetAsync(predicate: entity => entity.Id == congressId);

        if (congress is null)
            throw new BusinessException(CongressSlidersMessages.CongressNotFound);
    }

    public Task CongressSliderShouldExistWhenSelected(CongressSlider? entity)
    {
        if (entity is null)
            throw new BusinessException(CongressSlidersMessages.EntityNotFound);

        return Task.CompletedTask;
    }

    public Task SliderShouldBelongToCongress(CongressSlider entity, Guid congressId)
    {
        if (congressId == Guid.Empty || entity.CongressId != congressId)
            throw new BusinessException(CongressSlidersMessages.EntityNotFound);

        return Task.CompletedTask;
    }

    public Task OrderShouldBeValid(int order)
    {
        if (order < 0)
            throw new BusinessException(CongressSlidersMessages.InvalidOrder);

        return Task.CompletedTask;
    }

    public Task ImagePathShouldExist(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            throw new BusinessException(CongressSlidersMessages.ImageRequired);

        return Task.CompletedTask;
    }


    public Task ImageShouldBeValid(CongressSliderImageInputDto? image, bool isRequired)
    {
        if (image is null || image.Length <= 0)
        {
            if (isRequired)
                throw new BusinessException(CongressSlidersMessages.ImageRequired);

            return Task.CompletedTask;
        }

        BackOfficeObjectStorageHelper.ValidateImage(
            image.OriginalFileName,
            image.Length,
            isRequired,
            CongressSlidersMessages.ImageRequired,
            CongressSlidersMessages.ImageInvalid);

        return Task.CompletedTask;
    }

    public Task TranslationFieldsShouldBeValid(IEnumerable<TranslationInputDto> translations)
    {
        foreach (TranslationInputDto translation in translations)
        {
            string? title = GetFieldValue(translation, "Title");
            string? subtitle = GetFieldValue(translation, "Subtitle");
            string? buttonText = GetFieldValue(translation, "ButtonText");
            string? buttonUrl = GetFieldValue(translation, "ButtonUrl");

            if (title?.Length > 300)
                throw new BusinessException(CongressSlidersMessages.TitleMaxLengthExceeded);

            if (subtitle?.Length > 1000)
                throw new BusinessException(CongressSlidersMessages.SubtitleMaxLengthExceeded);

            if (buttonText?.Length > 120)
                throw new BusinessException(CongressSlidersMessages.ButtonTextMaxLengthExceeded);

            if (buttonUrl?.Length > 1000)
                throw new BusinessException(CongressSlidersMessages.ButtonUrlMaxLengthExceeded);
        }

        return Task.CompletedTask;
    }

    private static string? GetFieldValue(TranslationInputDto translation, string fieldName)
    {
        return translation.Fields.TryGetValue(fieldName, out string? value)
            ? value?.Trim()
            : null;
    }
}
