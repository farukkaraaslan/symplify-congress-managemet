using Core.Application.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressSliders.Commands.Create;
using Symplify.BackOffice.Application.Features.CongressSliders.Commands.Delete;
using Symplify.BackOffice.Application.Features.CongressSliders.Commands.Reorder;
using Symplify.BackOffice.Application.Features.CongressSliders.Commands.Update;
using Symplify.BackOffice.Application.Features.CongressSliders.Queries.GetForUpdate;
using Symplify.BackOffice.Application.Features.CongressSliders.Queries.GetList;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.WebUI.Extensions;
using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.WebUI.Models.CongressSliders;
using Symplify.BackOffice.WebUI.Models.Shared.DataTables;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture=tr-TR}/[controller]/[action]")]
public sealed class CongressSlidersController : Controller
{
    private const string SafeFallbackCulture = "tr-TR";
    private const long MaxImageSizeInBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".svg"
    };

    private const string TitleField = "Title";
    private const string SubtitleField = "Subtitle";
    private const string ButtonTextField = "ButtonText";
    private const string ButtonUrlField = "ButtonUrl";

    private readonly IMediator _mediator;
    private readonly IApplicationLanguageProvider _applicationLanguageProvider;
    private readonly IBackOfficeViewLocalizer _localizer;

    public CongressSlidersController(
        IMediator mediator,
        IApplicationLanguageProvider applicationLanguageProvider,
        IBackOfficeViewLocalizer localizer)
    {
        _mediator = mediator;
        _applicationLanguageProvider = applicationLanguageProvider;
        _localizer = localizer;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GetList(
        [FromForm] DataTableRequest request,
        [FromForm] Guid congressId,
        CancellationToken cancellationToken)
    {
        DataTableQueryOptions tableOptions = DataTableQueryOptions.From(
            request,
            defaultSortColumn: "order",
            defaultSortDirection: "asc",
            allowedSortColumns: new[] { "order", "title", "isActive" });

        string culture = await ResolveCurrentCultureAsync(cancellationToken);

        var response = await _mediator.Send(
            new GetListCongressSliderQuery
            {
                CongressId = congressId,
                Culture = culture,
                SearchText = tableOptions.SearchText,
                SortColumn = tableOptions.SortColumn,
                SortDirection = tableOptions.SortDirection,
                PageRequest = new PageRequest
                {
                    Page = tableOptions.Page,
                    PageSize = tableOptions.PageSize
                }
            },
            cancellationToken);

        return Json(new
        {
            draw = request.Draw,
            recordsTotal = response.Count,
            recordsFiltered = response.Count,
            data = response.Items.Select((item, index) => new
            {
                rowNumber = tableOptions.Start + index + 1,
                id = item.Id,
                congressId = item.CongressId,
                imagePath = item.ImagePreviewUrl ?? item.ImagePath,
                imageObjectPath = item.ImagePath,
                order = item.Order,
                title = item.Title,
                subtitle = item.Subtitle,
                buttonText = item.ButtonText,
                buttonUrl = item.ButtonUrl,
                isActive = item.IsActive,
                isFallback = item.IsFallback
            })
        });
    }

    [HttpGet]
    public async Task<IActionResult> CreateModal(Guid congressId, CancellationToken cancellationToken)
    {
        CreateCongressSliderViewModel model = await BuildCreateViewModelAsync(congressId, cancellationToken);
        return PartialView("~/Views/CongressSliders/_CreateSliderModal.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] CreateCongressSliderViewModel model, CancellationToken cancellationToken)
    {
        ValidateCreateModel(model);

        if (!ModelState.IsValid)
            return BadRequest(CreateValidationErrorResponse());

        try
        {
            CreatedCongressSliderResponse response = await _mediator.Send(
                new CreateCongressSliderCommand
                {
                    CongressId = model.CongressId,
                    Image = model.ImageFile.ToCongressSliderImageInputDto(),
                    IsActive = model.IsActive,
                    Translations = BuildTranslationInputs(model.Translations)
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                id = response.Id,
                message = GetText("BackOffice.CongressSliders.Messages.Created", "Slider başarıyla oluşturuldu.")
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new
            {
                success = false,
                message = GetExceptionMessage(exception)
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditModal(Guid id, Guid congressId, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(
            new GetCongressSliderForUpdateQuery
            {
                Id = id,
                CongressId = congressId
            },
            cancellationToken);

        UpdateCongressSliderViewModel model = new()
        {
            Id = response.Id,
            CongressId = response.CongressId,
            ImagePath = response.ImagePath,
            ImagePreviewUrl = response.ImagePreviewUrl,
            Order = response.Order,
            IsActive = response.IsActive,
            Translations = response.Translations.Select(translation => new CongressSliderTranslationViewModel
            {
                LanguageId = translation.LanguageId,
                Culture = translation.Culture,
                LanguageName = translation.LanguageName,
                IsDefault = translation.IsDefault,
                Exists = translation.Exists,
                Title = GetField(translation.Fields, TitleField),
                Subtitle = GetField(translation.Fields, SubtitleField),
                ButtonText = GetField(translation.Fields, ButtonTextField),
                ButtonUrl = GetField(translation.Fields, ButtonUrlField)
            }).ToList()
        };

        return PartialView("~/Views/CongressSliders/_UpdateSliderModal.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromForm] UpdateCongressSliderViewModel model, CancellationToken cancellationToken)
    {
        ValidateUpdateModel(model);

        if (!ModelState.IsValid)
            return BadRequest(CreateValidationErrorResponse());

        try
        {
            await _mediator.Send(
                new UpdateCongressSliderCommand
                {
                    Id = model.Id,
                    CongressId = model.CongressId,
                    ImagePath = model.ImagePath,
                    Image = model.ImageFile.ToCongressSliderImageInputDto(),
                    IsActive = model.IsActive,
                    Translations = BuildTranslationInputs(model.Translations)
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("BackOffice.CongressSliders.Messages.Updated", "Slider başarıyla güncellendi.")
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new
            {
                success = false,
                message = GetExceptionMessage(exception)
            });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([FromForm] Guid id, [FromForm] Guid congressId, CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new DeleteCongressSliderCommand { Id = id }, cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("BackOffice.CongressSliders.Messages.Deleted", "Slider başarıyla silindi.")
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new
            {
                success = false,
                message = GetExceptionMessage(exception)
            });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder(
        [FromBody] DataTableReorderRequest request,
        [FromQuery] Guid congressId,
        CancellationToken cancellationToken)
    {
        if (congressId == Guid.Empty || request is null || request.Items.Count == 0)
        {
            return BadRequest(new
            {
                success = false,
                message = GetText("Common.InvalidRequest", "Geçersiz istek.")
            });
        }

        try
        {
            await _mediator.Send(
                new ReorderCongressSliderCommand
                {
                    CongressId = congressId,
                    Items = request.Items
                        .Where(item => item.Id != Guid.Empty)
                        .Select(item => new ReorderCongressSliderItemDto
                        {
                            Id = item.Id,
                            Order = item.Order
                        })
                        .ToList()
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("Common.ReorderSuccess", "Sıralama güncellendi.")
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new
            {
                success = false,
                message = GetExceptionMessage(exception)
            });
        }
    }

    private async Task<CreateCongressSliderViewModel> BuildCreateViewModelAsync(Guid congressId, CancellationToken cancellationToken)
    {
        IReadOnlyList<ApplicationLanguageDto> languages = await _applicationLanguageProvider.GetActiveLanguagesAsync(cancellationToken);

        return new CreateCongressSliderViewModel
        {
            CongressId = congressId,
            IsActive = true,
            Translations = languages
                .OrderByDescending(language => language.IsDefault)
                .ThenBy(language => language.Name)
                .Select(language => new CongressSliderTranslationViewModel
                {
                    LanguageId = language.Id,
                    Culture = language.Culture,
                    LanguageName = language.Name,
                    IsDefault = language.IsDefault
                })
                .ToList()
        };
    }

    private void ValidateCreateModel(CreateCongressSliderViewModel model)
    {
        ValidateBaseModel(model.CongressId, model.Translations);

        if (model.ImageFile is null || model.ImageFile.Length == 0)
        {
            ModelState.AddModelError(nameof(model.ImageFile), GetText("BackOffice.CongressSliders.Validation.ImageRequired", "Slider görseli zorunludur."));
            return;
        }

        ValidateImageFile(model.ImageFile, nameof(model.ImageFile));
    }

    private void ValidateUpdateModel(UpdateCongressSliderViewModel model)
    {
        if (model.Id == Guid.Empty)
            ModelState.AddModelError(nameof(model.Id), GetText("Common.InvalidRequest", "Geçersiz istek."));

        if (string.IsNullOrWhiteSpace(model.ImagePath) && (model.ImageFile is null || model.ImageFile.Length == 0))
            ModelState.AddModelError(nameof(model.ImageFile), GetText("BackOffice.CongressSliders.Validation.ImageRequired", "Slider görseli zorunludur."));

        if (model.ImageFile is not null && model.ImageFile.Length > 0)
            ValidateImageFile(model.ImageFile, nameof(model.ImageFile));

        ValidateBaseModel(model.CongressId, model.Translations);
    }

    private void ValidateBaseModel(Guid congressId, List<CongressSliderTranslationViewModel> translations)
    {
        if (congressId == Guid.Empty)
            ModelState.AddModelError(nameof(CreateCongressSliderViewModel.CongressId), GetText("BackOffice.CongressSliders.Validation.CongressRequired", "Kongre bilgisi zorunludur."));

        int defaultLanguageIndex = translations.FindIndex(translation => translation.IsDefault);

        if (defaultLanguageIndex < 0)
        {
            ModelState.AddModelError(nameof(CreateCongressSliderViewModel.Translations), GetText("Common.InvalidRequest", "Geçersiz istek."));
            return;
        }

        for (int index = 0; index < translations.Count; index++)
        {
            CongressSliderTranslationViewModel translation = translations[index];

            AddMaxLengthErrorIfExceeded(
                $"Translations[{index}].Title",
                translation.Title,
                300,
                "BackOffice.CongressSliders.Validation.TitleMaxLengthExceeded",
                "Slider başlığı en fazla 300 karakter olabilir.");

            AddMaxLengthErrorIfExceeded(
                $"Translations[{index}].Subtitle",
                translation.Subtitle,
                1000,
                "BackOffice.CongressSliders.Validation.SubtitleMaxLengthExceeded",
                "Slider alt başlığı en fazla 1000 karakter olabilir.");

            AddMaxLengthErrorIfExceeded(
                $"Translations[{index}].ButtonText",
                translation.ButtonText,
                120,
                "BackOffice.CongressSliders.Validation.ButtonTextMaxLengthExceeded",
                "Buton metni en fazla 120 karakter olabilir.");

            AddMaxLengthErrorIfExceeded(
                $"Translations[{index}].ButtonUrl",
                translation.ButtonUrl,
                1000,
                "BackOffice.CongressSliders.Validation.ButtonUrlMaxLengthExceeded",
                "Buton URL değeri en fazla 1000 karakter olabilir.");
        }
    }

    private void AddMaxLengthErrorIfExceeded(string key, string? value, int maxLength, string messageKey, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
            ModelState.AddModelError(key, GetText(messageKey, fallback));
    }

    private void ValidateImageFile(IFormFile file, string key)
    {
        string extension = Path.GetExtension(file.FileName);

        if (!AllowedImageExtensions.Contains(extension))
        {
            ModelState.AddModelError(key, GetText("BackOffice.CongressSliders.Validation.ImageExtensionInvalid", "Sadece JPG, PNG, WEBP veya SVG görsel yükleyebilirsiniz."));
        }

        if (file.Length > MaxImageSizeInBytes)
        {
            ModelState.AddModelError(key, GetText("BackOffice.CongressSliders.Validation.ImageSizeInvalid", "Slider görseli en fazla 5 MB olabilir."));
        }
    }

    private ICollection<TranslationInputDto> BuildTranslationInputs(IEnumerable<CongressSliderTranslationViewModel> translations)
    {
        return translations
            .GroupBy(translation => translation.LanguageId)
            .Select(group => group.First())
            .Where(translation => translation.Exists || HasAnyTranslationValue(translation))
            .Select(translation => new TranslationInputDto
            {
                LanguageId = translation.LanguageId,
                Fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    [TitleField] = NormalizeText(translation.Title),
                    [SubtitleField] = NormalizeText(translation.Subtitle),
                    [ButtonTextField] = NormalizeText(translation.ButtonText),
                    [ButtonUrlField] = NormalizeText(translation.ButtonUrl)
                }
            })
            .ToList();
    }

    private static bool HasAnyTranslationValue(CongressSliderTranslationViewModel translation)
    {
        return !string.IsNullOrWhiteSpace(translation.Title) ||
               !string.IsNullOrWhiteSpace(translation.Subtitle) ||
               !string.IsNullOrWhiteSpace(translation.ButtonText) ||
               !string.IsNullOrWhiteSpace(translation.ButtonUrl);
    }

    private async Task<string> ResolveCurrentCultureAsync(CancellationToken cancellationToken)
    {
        string? routeCulture = RouteData.Values["culture"]?.ToString();

        if (!string.IsNullOrWhiteSpace(routeCulture))
            return await NormalizeCultureFromApplicationLanguagesAsync(routeCulture, cancellationToken);

        string? pathCulture = HttpContext.Request.Path.Value?
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        return await NormalizeCultureFromApplicationLanguagesAsync(pathCulture, cancellationToken);
    }

    private async Task<string> NormalizeCultureFromApplicationLanguagesAsync(string? culture, CancellationToken cancellationToken)
    {
        IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _applicationLanguageProvider.GetActiveLanguagesAsync(cancellationToken);
        string? requestedCulture = culture?.Trim();

        if (!string.IsNullOrWhiteSpace(requestedCulture))
        {
            ApplicationLanguageDto? matchedLanguage = activeLanguages
                .OrderByDescending(language => language.IsDefault)
                .ThenBy(language => language.Name)
                .FirstOrDefault(language =>
                    string.Equals(language.Culture, requestedCulture, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(GetTwoLetterIsoCode(language.Culture), requestedCulture, StringComparison.OrdinalIgnoreCase));

            if (matchedLanguage is not null)
                return matchedLanguage.Culture;
        }

        ApplicationLanguageDto defaultLanguage = await _applicationLanguageProvider.GetDefaultLanguageAsync(cancellationToken);

        return !string.IsNullOrWhiteSpace(defaultLanguage.Culture)
            ? defaultLanguage.Culture
            : activeLanguages.OrderByDescending(language => language.IsDefault).ThenBy(language => language.Name).FirstOrDefault()?.Culture ?? SafeFallbackCulture;
    }

    private static string GetTwoLetterIsoCode(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
            return string.Empty;

        string normalizedCulture = culture.Trim();
        int separatorIndex = normalizedCulture.IndexOf('-');

        return separatorIndex > 0 ? normalizedCulture[..separatorIndex] : normalizedCulture;
    }

    private string GetText(string key, string fallback)
    {
        string value = _localizer.GetStringValue(key);

        return string.IsNullOrWhiteSpace(value) ? key : value;
    }

    private static string? GetField(IReadOnlyDictionary<string, string?> fields, string key)
    {
        return fields.TryGetValue(key, out string? value) ? value : null;
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private string GetExceptionMessage(Exception exception)
    {
        return !string.IsNullOrWhiteSpace(exception.Message)
            ? GetText(exception.Message, exception.Message)
            : GetText("Common.GenericError", string.Empty);
    }

    private object CreateValidationErrorResponse()
    {
        return new
        {
            success = false,
            message = GetText("Common.InvalidRequest", "Form alanlarını kontrol edin."),
            errors = ModelState
                .Where(item => item.Value?.Errors.Count > 0)
                .ToDictionary(
                    item => item.Key,
                    item => item.Value!.Errors.Select(error => GetText(string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Common.InvalidRequest" : error.ErrorMessage, error.ErrorMessage)).ToArray())
        };
    }
}
