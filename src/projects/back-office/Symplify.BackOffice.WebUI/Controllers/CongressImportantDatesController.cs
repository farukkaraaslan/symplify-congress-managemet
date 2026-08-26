using System.Globalization;
using Core.Application.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressImportantDates.Commands.Create;
using Symplify.BackOffice.Application.Features.CongressImportantDates.Commands.Delete;
using Symplify.BackOffice.Application.Features.CongressImportantDates.Commands.Reorder;
using Symplify.BackOffice.Application.Features.CongressImportantDates.Commands.Update;
using Symplify.BackOffice.Application.Features.CongressImportantDates.Queries.GetForUpdate;
using Symplify.BackOffice.Application.Features.CongressImportantDates.Queries.GetList;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.WebUI.Models.CongressImportantDates;
using Symplify.BackOffice.WebUI.Models.Shared.DataTables;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture=tr-TR}/[controller]/[action]")]
public sealed class CongressImportantDatesController : Controller
{
    private const string SafeFallbackCulture = "tr-TR";
    private const string TitleField = "Title";
    private const string DescriptionField = "Description";

    private readonly IMediator _mediator;
    private readonly IApplicationLanguageProvider _applicationLanguageProvider;
    private readonly IBackOfficeViewLocalizer _localizer;

    public CongressImportantDatesController(
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
        if (congressId == Guid.Empty)
        {
            return Json(new
            {
                draw = request.Draw,
                recordsTotal = 0,
                recordsFiltered = 0,
                data = Array.Empty<object>()
            });
        }

        DataTableQueryOptions tableOptions = DataTableQueryOptions.From(
            request,
            defaultSortColumn: "order",
            defaultSortDirection: "asc",
            allowedSortColumns: new[] { "order", "startDate", "endDate", "title", "isActive" });

        string culture = await ResolveCurrentCultureAsync(cancellationToken);

        var response = await _mediator.Send(
            new GetListCongressImportantDateQuery
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
                startDate = FormatDateTime(item.StartDate),
                startDateIso = item.StartDate.ToString("O"),
                endDate = FormatDateTime(item.EndDate),
                endDateIso = item.EndDate.ToString("O"),
                order = item.Order,
                title = item.Title,
                description = item.Description,
                culture,
                isActive = item.IsActive,
                isFallback = item.IsFallback
            })
        });
    }

    [HttpGet]
    public async Task<IActionResult> CreateModal(Guid congressId, CancellationToken cancellationToken)
    {
        CreateCongressImportantDateViewModel model = await BuildCreateViewModelAsync(congressId, cancellationToken);
        return PartialView("~/Views/CongressImportantDates/_CreateImportantDateModal.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromForm] CreateCongressImportantDateViewModel model,
        CancellationToken cancellationToken)
    {
        DateRange? dateRange = ValidateCreateModel(model);

        if (!ModelState.IsValid || dateRange is null)
            return BadRequest(CreateValidationErrorResponse());

        try
        {
            CreatedCongressImportantDateResponse response = await _mediator.Send(
                new CreateCongressImportantDateCommand
                {
                    CongressId = model.CongressId,
                    StartDate = dateRange.StartDate,
                    EndDate = dateRange.EndDate,
                    Order = model.Order,
                    IsActive = model.IsActive,
                    Translations = BuildTranslationInputs(model.Translations)
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                id = response.Id,
                message = GetText("BackOffice.CongressImportantDates.Messages.Created", "Önemli tarih başarıyla oluşturuldu.")
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
            new GetCongressImportantDateForUpdateQuery
            {
                Id = id,
                CongressId = congressId
            },
            cancellationToken);

        UpdateCongressImportantDateViewModel model = new()
        {
            Id = response.Id,
            CongressId = response.CongressId,
            StartDateText = FormatDateTime(response.StartDate),
            EndDateText = FormatDateTime(response.EndDate),
            Order = response.Order,
            IsActive = response.IsActive,
            Translations = response.Translations.Select(translation => new CongressImportantDateTranslationViewModel
            {
                LanguageId = translation.LanguageId,
                Culture = translation.Culture,
                LanguageName = translation.LanguageName,
                IsDefault = translation.IsDefault,
                Exists = translation.Exists,
                Title = GetField(translation.Fields, TitleField),
                Description = GetField(translation.Fields, DescriptionField)
            }).ToList()
        };

        return PartialView("~/Views/CongressImportantDates/_UpdateImportantDateModal.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        [FromForm] UpdateCongressImportantDateViewModel model,
        CancellationToken cancellationToken)
    {
        DateRange? dateRange = ValidateUpdateModel(model);

        if (!ModelState.IsValid || dateRange is null)
            return BadRequest(CreateValidationErrorResponse());

        try
        {
            await _mediator.Send(
                new UpdateCongressImportantDateCommand
                {
                    Id = model.Id,
                    CongressId = model.CongressId,
                    StartDate = dateRange.StartDate,
                    EndDate = dateRange.EndDate,
                    Order = model.Order,
                    IsActive = model.IsActive,
                    Translations = BuildTranslationInputs(model.Translations)
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("BackOffice.CongressImportantDates.Messages.Updated", "Önemli tarih başarıyla güncellendi.")
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
        if (id == Guid.Empty || congressId == Guid.Empty)
        {
            return BadRequest(new
            {
                success = false,
                message = GetText("Common.InvalidRequest", "Geçersiz istek.")
            });
        }

        try
        {
            try
            {
                await _mediator.Send(
                    new GetCongressImportantDateForUpdateQuery
                    {
                        Id = id,
                        CongressId = congressId
                    },
                    cancellationToken);
            }
            catch
            {
                // Delete command will return the authoritative business error.
            }

            await _mediator.Send(new DeleteCongressImportantDateCommand { Id = id }, cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("BackOffice.CongressImportantDates.Messages.Deleted", "Önemli tarih başarıyla silindi.")
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
                new ReorderCongressImportantDateCommand
                {
                    CongressId = congressId,
                    Items = request.Items
                        .Where(item => item.Id != Guid.Empty)
                        .Select(item => new ReorderCongressImportantDateItemDto
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
                message = GetText("BackOffice.CongressImportantDates.Messages.Reordered", "Önemli tarih sıralaması güncellendi.")
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

    private async Task<CreateCongressImportantDateViewModel> BuildCreateViewModelAsync(Guid congressId, CancellationToken cancellationToken)
    {
        IReadOnlyList<ApplicationLanguageDto> languages = await _applicationLanguageProvider.GetActiveLanguagesAsync(cancellationToken);
        int nextOrder = 1;

        if (congressId != Guid.Empty)
        {
            var list = await _mediator.Send(
                new GetListCongressImportantDateQuery
                {
                    CongressId = congressId,
                    PageRequest = new PageRequest
                    {
                        Page = 0,
                        PageSize = 500
                    }
                },
                cancellationToken);

            nextOrder = list.Count + 1;
        }

        DateTime now = DateTime.Now;

        return new CreateCongressImportantDateViewModel
        {
            CongressId = congressId,
            StartDateText = FormatDateTime(now),
            EndDateText = FormatDateTime(now.AddHours(1)),
            Order = nextOrder,
            IsActive = true,
            Translations = languages
                .OrderByDescending(language => language.IsDefault)
                .ThenBy(language => language.Name)
                .Select(language => new CongressImportantDateTranslationViewModel
                {
                    LanguageId = language.Id,
                    Culture = language.Culture,
                    LanguageName = language.Name,
                    IsDefault = language.IsDefault
                })
                .ToList()
        };
    }

    private DateRange? ValidateCreateModel(CreateCongressImportantDateViewModel model)
    {
        return ValidateBaseModel(model.CongressId, model.StartDateText, model.EndDateText, model.Order, model.Translations);
    }

    private DateRange? ValidateUpdateModel(UpdateCongressImportantDateViewModel model)
    {
        if (model.Id == Guid.Empty)
            ModelState.AddModelError(nameof(model.Id), GetText("Common.InvalidRequest", "Geçersiz istek."));

        return ValidateBaseModel(model.CongressId, model.StartDateText, model.EndDateText, model.Order, model.Translations);
    }

    private DateRange? ValidateBaseModel(
        Guid congressId,
        string? startDateText,
        string? endDateText,
        int order,
        List<CongressImportantDateTranslationViewModel> translations)
    {
        if (congressId == Guid.Empty)
            ModelState.AddModelError(nameof(CreateCongressImportantDateViewModel.CongressId), GetText("BackOffice.CongressImportantDates.Validation.CongressRequired", "Kongre bilgisi zorunludur."));

        DateTime? parsedStartDate = ParseDateTime(startDateText);
        DateTime? parsedEndDate = ParseDateTime(endDateText);

        if (!parsedStartDate.HasValue)
            ModelState.AddModelError(nameof(CreateCongressImportantDateViewModel.StartDateText), GetText("BackOffice.CongressImportantDates.Validation.StartDateRequired", "Başlangıç tarihi zorunludur."));

        if (!parsedEndDate.HasValue)
            ModelState.AddModelError(nameof(CreateCongressImportantDateViewModel.EndDateText), GetText("BackOffice.CongressImportantDates.Validation.EndDateRequired", "Bitiş tarihi zorunludur."));

        if (parsedStartDate.HasValue && parsedEndDate.HasValue && parsedEndDate.Value < parsedStartDate.Value)
            ModelState.AddModelError(nameof(CreateCongressImportantDateViewModel.EndDateText), GetText("BackOffice.CongressImportantDates.Validation.DateRangeInvalid", "Bitiş tarihi başlangıç tarihinden önce olamaz."));

        if (order < 0)
            ModelState.AddModelError(nameof(CreateCongressImportantDateViewModel.Order), GetText("BackOffice.CongressImportantDates.Validation.OrderInvalid", "Sıralama değeri sıfırdan küçük olamaz."));

        int defaultLanguageIndex = translations.FindIndex(translation => translation.IsDefault);

        if (defaultLanguageIndex < 0)
        {
            ModelState.AddModelError(nameof(CreateCongressImportantDateViewModel.Translations), GetText("Common.InvalidRequest", "Geçersiz istek."));
            return parsedStartDate.HasValue && parsedEndDate.HasValue
                ? new DateRange(parsedStartDate.Value, parsedEndDate.Value)
                : null;
        }

        CongressImportantDateTranslationViewModel defaultTranslation = translations[defaultLanguageIndex];

        if (string.IsNullOrWhiteSpace(defaultTranslation.Title))
        {
            ModelState.AddModelError(
                $"Translations[{defaultLanguageIndex}].Title",
                GetText("BackOffice.CongressImportantDates.Validation.TitleRequired", "Varsayılan dilde tarih başlığı zorunludur."));
        }

        for (int index = 0; index < translations.Count; index++)
        {
            CongressImportantDateTranslationViewModel translation = translations[index];

            bool hasAnyValue =
                !string.IsNullOrWhiteSpace(translation.Title) ||
                !string.IsNullOrWhiteSpace(translation.Description);

            if (!translation.IsDefault && hasAnyValue && string.IsNullOrWhiteSpace(translation.Title))
            {
                ModelState.AddModelError(
                    $"Translations[{index}].Title",
                    GetText("BackOffice.CongressImportantDates.Validation.TranslationTitleRequired", "Bu dil için herhangi bir açıklama girildiyse tarih başlığı da zorunludur."));
            }
        }

        return parsedStartDate.HasValue && parsedEndDate.HasValue
            ? new DateRange(parsedStartDate.Value, parsedEndDate.Value)
            : null;
    }

    private ICollection<TranslationInputDto> BuildTranslationInputs(
        IEnumerable<CongressImportantDateTranslationViewModel> translations)
    {
        return translations
            .GroupBy(translation => translation.LanguageId)
            .Select(group => group.First())
            .Where(HasAnyTranslationValue)
            .Select(translation => new TranslationInputDto
            {
                LanguageId = translation.LanguageId,
                Fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    [TitleField] = NormalizeText(translation.Title),
                    [DescriptionField] = NormalizeText(translation.Description)
                }
            })
            .ToList();
    }

    private static bool HasAnyTranslationValue(CongressImportantDateTranslationViewModel translation)
    {
        return !string.IsNullOrWhiteSpace(translation.Title) ||
               !string.IsNullOrWhiteSpace(translation.Description);
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

    private string GetExceptionMessage(Exception exception)
    {
        return !string.IsNullOrWhiteSpace(exception.Message)
            ? GetText(exception.Message, exception.Message)
            : GetText("Common.GenericError", string.Empty);
    }

    private static string? GetField(IReadOnlyDictionary<string, string?> fields, string key)
    {
        return fields.TryGetValue(key, out string? value) ? value : null;
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string FormatDateTime(DateTime value)
    {
        return value.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
    }

    private static DateTime? ParseDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        string normalizedValue = value.Trim();

        string[] formats =
        {
            "dd.MM.yyyy HH:mm",
            "dd.MM.yyyy H:mm",
            "dd.MM.yyyy",
            "yyyy-MM-ddTHH:mm",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-dd"
        };

        CultureInfo trCulture = CultureInfo.GetCultureInfo("tr-TR");

        if (DateTime.TryParseExact(
                normalizedValue,
                formats,
                trCulture,
                DateTimeStyles.AssumeLocal,
                out DateTime parsedExact))
        {
            return parsedExact;
        }

        if (DateTime.TryParse(
                normalizedValue,
                trCulture,
                DateTimeStyles.AssumeLocal,
                out DateTime parsed))
        {
            return parsed;
        }

        return null;
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
                    item => item.Value!.Errors.Select(error =>
                        GetText(string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Common.InvalidRequest" : error.ErrorMessage, error.ErrorMessage)).ToArray())
        };
    }

    private sealed record DateRange(DateTime StartDate, DateTime EndDate);
}
