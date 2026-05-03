using Core.Application.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.TransactionStatusPhases.Queries.GetList;
using Symplify.BackOffice.Application.Features.TransactionStatuses.Commands.Create;
using Symplify.BackOffice.Application.Features.TransactionStatuses.Commands.Delete;
using Symplify.BackOffice.Application.Features.TransactionStatuses.Commands.Reorder;
using Symplify.BackOffice.Application.Features.TransactionStatuses.Commands.Update;
using Symplify.BackOffice.Application.Features.TransactionStatuses.Queries.GetForUpdate;
using Symplify.BackOffice.Application.Features.TransactionStatuses.Queries.GetList;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.WebUI.Models.Shared.DataTables;
using Symplify.BackOffice.WebUI.Models.TransactionStatuses;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture=tr-TR}/[controller]/[action]")]
public sealed class TransactionStatusesController : Controller
{
    private const string SafeFallbackCulture = "tr-TR";
    private const string NameField = "Name";
    private const string DescriptionField = "Description";

    private readonly IMediator _mediator;
    private readonly IApplicationLanguageProvider _applicationLanguageProvider;
    private readonly IBackOfficeViewLocalizer _localizer;

    public TransactionStatusesController(
        IMediator mediator,
        IApplicationLanguageProvider applicationLanguageProvider,
        IBackOfficeViewLocalizer localizer)
    {
        _mediator = mediator;
        _applicationLanguageProvider = applicationLanguageProvider;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        TransactionStatusesIndexViewModel model = await BuildIndexViewModelAsync(cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GetList([FromForm] DataTableRequest request, int? transactionStatusPhaseId, CancellationToken cancellationToken)
    {
        string culture = await ResolveCurrentCultureAsync(cancellationToken);

        DataTableQueryOptions tableOptions = DataTableQueryOptions.From(
            request,
            defaultSortColumn: transactionStatusPhaseId.HasValue ? "order" : "phase",
            defaultSortDirection: "asc",
            allowedSortColumns: new[] { "phase", "phaseName", "transactionStatusPhaseId", "order", "code", "name", "isEditable", "isFinal", "isActive" });

        var response = await _mediator.Send(
            new GetListTransactionStatusQuery
            {
                Culture = culture,
                TransactionStatusPhaseId = transactionStatusPhaseId,
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

        List<object> pageItems = response.Items.Select((item, index) => new
        {
            rowNumber = tableOptions.Start + index + 1,
            id = item.Id,
            transactionStatusPhaseId = item.TransactionStatusPhaseId,
            transactionStatusPhaseCode = item.TransactionStatusPhaseCode,
            transactionStatusPhaseName = item.TransactionStatusPhaseName,
            order = item.Order,
            code = item.Code,
            name = item.Name,
            description = item.Description,
            isEditable = item.IsEditable,
            isFinal = item.IsFinal,
            isActive = item.IsActive
        }).Cast<object>().ToList();

        return Json(new
        {
            draw = request.Draw,
            recordsTotal = response.Count,
            recordsFiltered = response.Count,
            data = pageItems
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetForUpdate(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(new { success = false, message = GetText("Common.InvalidRequest", "Geçersiz istek.") });
        }

        try
        {
            GetTransactionStatusForUpdateResponse response = await _mediator.Send(
                new GetTransactionStatusForUpdateQuery { Id = id },
                cancellationToken);

            return Json(new
            {
                success = true,
                id = response.Id,
                transactionStatusPhaseId = response.TransactionStatusPhaseId,
                code = response.Code,
                order = response.Order,
                isEditable = response.IsEditable,
                isFinal = response.IsFinal,
                isActive = response.IsActive,
                translations = response.Translations.Select(translation => new
                {
                    languageId = translation.LanguageId,
                    culture = translation.Culture,
                    languageName = translation.LanguageName,
                    isDefault = translation.IsDefault,
                    exists = translation.Exists,
                    name = GetTranslationField(translation, NameField),
                    description = GetTranslationField(translation, DescriptionField)
                }).ToList()
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new { success = false, message = GetExceptionMessage(exception) });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] CreateTransactionStatusViewModel model, CancellationToken cancellationToken)
    {
        ValidateCreateModel(model);

        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                message = GetText("Common.InvalidRequest", "Geçersiz istek."),
                errors = GetModelStateErrors()
            });
        }

        try
        {
            await _mediator.Send(
                new CreateTransactionStatusCommand
                {
                    TransactionStatusPhaseId = model.TransactionStatusPhaseId,
                    Code = NormalizeText(model.Code) ?? string.Empty,
                    IsEditable = model.IsEditable,
                    IsFinal = model.IsFinal,
                    IsActive = model.IsActive,
                    Translations = BuildTranslationInputs(model.Translations)
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("BackOffice.TransactionStatuses.Messages.Created", "Durum oluşturuldu.")
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new { success = false, message = GetExceptionMessage(exception) });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromForm] UpdateTransactionStatusViewModel model, CancellationToken cancellationToken)
    {
        ValidateUpdateModel(model);

        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                message = GetText("Common.InvalidRequest", "Geçersiz istek."),
                errors = GetModelStateErrors()
            });
        }

        try
        {
            await _mediator.Send(
                new UpdateTransactionStatusCommand
                {
                    Id = model.Id,
                    TransactionStatusPhaseId = model.TransactionStatusPhaseId,
                    Code = NormalizeText(model.Code) ?? string.Empty,
                    IsEditable = model.IsEditable,
                    IsFinal = model.IsFinal,
                    IsActive = model.IsActive,
                    Translations = BuildTranslationInputs(model.Translations)
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("BackOffice.TransactionStatuses.Messages.Updated", "Durum güncellendi.")
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new { success = false, message = GetExceptionMessage(exception) });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(new { success = false, message = GetText("Common.InvalidRequest", "Geçersiz istek.") });
        }

        try
        {
            await _mediator.Send(new DeleteTransactionStatusCommand { Id = id }, cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("BackOffice.TransactionStatuses.Messages.Deleted", "Durum silindi.")
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new { success = false, message = GetExceptionMessage(exception) });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder([FromBody] DataTableIntReorderRequest request, CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0 || !request.TransactionStatusPhaseId.HasValue || request.TransactionStatusPhaseId.Value <= 0)
        {
            return BadRequest(new
            {
                success = false,
                message = GetText("BackOffice.TransactionStatuses.Validation.ReorderDifferentPhaseNotAllowed", "Sıralama için önce faz filtresi seçilmelidir.")
            });
        }

        try
        {
            await _mediator.Send(
                new ReorderTransactionStatusCommand
                {
                    TransactionStatusPhaseId = request.TransactionStatusPhaseId,
                    Items = request.Items
                        .Where(item => item.Id > 0)
                        .Select(item => new ReorderTransactionStatusItemDto { Id = item.Id, Order = item.Order })
                        .ToList()
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("BackOffice.TransactionStatuses.Messages.Reordered", "Sıralama güncellendi.")
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new { success = false, message = GetExceptionMessage(exception) });
        }
    }

    private async Task<TransactionStatusesIndexViewModel> BuildIndexViewModelAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ApplicationLanguageDto> languages = await _applicationLanguageProvider.GetActiveLanguagesAsync(cancellationToken);

        List<ApplicationLanguageDto> orderedLanguages = languages
            .OrderByDescending(language => language.IsDefault)
            .ThenBy(language => language.Name)
            .ToList();

        string culture = await ResolveCurrentCultureAsync(cancellationToken);
        List<TransactionStatusPhaseSelectItemViewModel> phases = await GetPhaseSelectItemsAsync(culture, cancellationToken);
        int defaultPhaseId = phases.FirstOrDefault()?.Id ?? 0;

        return new TransactionStatusesIndexViewModel
        {
            Phases = phases,
            CreateTransactionStatus = new CreateTransactionStatusViewModel
            {
                TransactionStatusPhaseId = defaultPhaseId,
                IsEditable = true,
                IsFinal = false,
                IsActive = true,
                Translations = orderedLanguages.Select(language => new CreateTransactionStatusTranslationViewModel
                {
                    LanguageId = language.Id,
                    Culture = language.Culture,
                    LanguageName = language.Name,
                    IsDefault = language.IsDefault
                }).ToList()
            },
            UpdateTransactionStatus = new UpdateTransactionStatusViewModel
            {
                TransactionStatusPhaseId = defaultPhaseId,
                IsEditable = true,
                IsFinal = false,
                IsActive = true,
                Translations = orderedLanguages.Select(language => new UpdateTransactionStatusTranslationViewModel
                {
                    LanguageId = language.Id,
                    Culture = language.Culture,
                    LanguageName = language.Name,
                    IsDefault = language.IsDefault,
                    Exists = false
                }).ToList()
            }
        };
    }

    private async Task<List<TransactionStatusPhaseSelectItemViewModel>> GetPhaseSelectItemsAsync(string culture, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(
            new GetListTransactionStatusPhaseQuery
            {
                Culture = culture,
                IsActive = true,
                SortColumn = "order",
                SortDirection = "asc",
                PageRequest = new PageRequest
                {
                    Page = 0,
                    PageSize = 500
                }
            },
            cancellationToken);

        return response.Items.Select(item => new TransactionStatusPhaseSelectItemViewModel
        {
            Id = item.Id,
            Code = item.Code,
            Name = item.Name
        }).ToList();
    }

    private void ValidateCreateModel(CreateTransactionStatusViewModel model)
    {
        if (model.TransactionStatusPhaseId <= 0)
        {
            ModelState.AddModelError(nameof(model.TransactionStatusPhaseId), GetText("BackOffice.TransactionStatuses.Validation.PhaseRequired", "Faz alanı zorunludur."));
        }

        if (string.IsNullOrWhiteSpace(model.Code))
        {
            ModelState.AddModelError(nameof(model.Code), GetText("BackOffice.TransactionStatuses.Validation.CodeRequired", "Kod alanı zorunludur."));
        }

        int defaultLanguageIndex = model.Translations.FindIndex(translation => translation.IsDefault);

        if (defaultLanguageIndex < 0)
        {
            ModelState.AddModelError(nameof(model.Translations), GetText("Common.InvalidRequest", "Geçersiz istek."));
            return;
        }

        if (string.IsNullOrWhiteSpace(model.Translations[defaultLanguageIndex].Name))
        {
            ModelState.AddModelError(
                $"Translations[{defaultLanguageIndex}].Name",
                GetText("BackOffice.TransactionStatuses.Validation.NameRequired", "Ad alanı zorunludur."));
        }
    }

    private void ValidateUpdateModel(UpdateTransactionStatusViewModel model)
    {
        if (model.Id <= 0)
        {
            ModelState.AddModelError(nameof(model.Id), GetText("Common.InvalidRequest", "Geçersiz istek."));
        }

        if (model.TransactionStatusPhaseId <= 0)
        {
            ModelState.AddModelError(nameof(model.TransactionStatusPhaseId), GetText("BackOffice.TransactionStatuses.Validation.PhaseRequired", "Faz alanı zorunludur."));
        }

        if (string.IsNullOrWhiteSpace(model.Code))
        {
            ModelState.AddModelError(nameof(model.Code), GetText("BackOffice.TransactionStatuses.Validation.CodeRequired", "Kod alanı zorunludur."));
        }

        int defaultLanguageIndex = model.Translations.FindIndex(translation => translation.IsDefault);

        if (defaultLanguageIndex < 0)
        {
            ModelState.AddModelError(nameof(model.Translations), GetText("Common.InvalidRequest", "Geçersiz istek."));
            return;
        }

        if (string.IsNullOrWhiteSpace(model.Translations[defaultLanguageIndex].Name))
        {
            ModelState.AddModelError(
                $"Translations[{defaultLanguageIndex}].Name",
                GetText("BackOffice.TransactionStatuses.Validation.NameRequired", "Ad alanı zorunludur."));
        }
    }

    private ICollection<TranslationInputDto> BuildTranslationInputs(IReadOnlyCollection<CreateTransactionStatusTranslationViewModel> translations)
    {
        return translations.GroupBy(translation => translation.LanguageId).Select(group => group.First()).Where(HasAnyTranslationValue).Select(translation => new TranslationInputDto
        {
            LanguageId = translation.LanguageId,
            Fields = BuildTranslationFields(translation)
        }).ToList();
    }

    private ICollection<TranslationInputDto> BuildTranslationInputs(IReadOnlyCollection<UpdateTransactionStatusTranslationViewModel> translations)
    {
        return translations.GroupBy(translation => translation.LanguageId).Select(group => group.First()).Where(HasAnyTranslationValue).Select(translation => new TranslationInputDto
        {
            LanguageId = translation.LanguageId,
            Fields = BuildTranslationFields(translation)
        }).ToList();
    }

    private static Dictionary<string, string?> BuildTranslationFields(CreateTransactionStatusTranslationViewModel translation)
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [NameField] = NormalizeText(translation.Name),
            [DescriptionField] = NormalizeText(translation.Description)
        };
    }

    private static Dictionary<string, string?> BuildTranslationFields(UpdateTransactionStatusTranslationViewModel translation)
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [NameField] = NormalizeText(translation.Name),
            [DescriptionField] = NormalizeText(translation.Description)
        };
    }

    private static bool HasAnyTranslationValue(CreateTransactionStatusTranslationViewModel translation)
    {
        return !string.IsNullOrWhiteSpace(translation.Name) || !string.IsNullOrWhiteSpace(translation.Description);
    }

    private static bool HasAnyTranslationValue(UpdateTransactionStatusTranslationViewModel translation)
    {
        return !string.IsNullOrWhiteSpace(translation.Name) || !string.IsNullOrWhiteSpace(translation.Description);
    }

    private Dictionary<string, string[]> GetModelStateErrors()
    {
        return ModelState.Where(item => item.Value is not null && item.Value.Errors.Count > 0).ToDictionary(
            item => item.Key,
            item => item.Value!.Errors.Select(error => error.ErrorMessage).ToArray());
    }

    private async Task<string> ResolveCurrentCultureAsync(CancellationToken cancellationToken)
    {
        string? formCulture = Request.HasFormContentType ? Request.Form["culture"].FirstOrDefault() : null;
        if (!string.IsNullOrWhiteSpace(formCulture)) return await NormalizeCultureFromApplicationLanguagesAsync(formCulture, cancellationToken);

        string? queryCulture = Request.Query["culture"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(queryCulture)) return await NormalizeCultureFromApplicationLanguagesAsync(queryCulture, cancellationToken);

        string? headerCulture = Request.Headers["X-Culture"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(headerCulture)) return await NormalizeCultureFromApplicationLanguagesAsync(headerCulture, cancellationToken);

        string? routeCulture = RouteData.Values["culture"]?.ToString();
        if (!string.IsNullOrWhiteSpace(routeCulture)) return await NormalizeCultureFromApplicationLanguagesAsync(routeCulture, cancellationToken);

        string? pathCulture = HttpContext.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return await NormalizeCultureFromApplicationLanguagesAsync(pathCulture, cancellationToken);
    }

    private async Task<string> NormalizeCultureFromApplicationLanguagesAsync(string? culture, CancellationToken cancellationToken)
    {
        IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _applicationLanguageProvider.GetActiveLanguagesAsync(cancellationToken);
        string? requestedCulture = culture?.Trim();

        if (!string.IsNullOrWhiteSpace(requestedCulture))
        {
            ApplicationLanguageDto? matchedLanguage = activeLanguages.OrderByDescending(language => language.IsDefault).ThenBy(language => language.Name).FirstOrDefault(language =>
                string.Equals(language.Culture, requestedCulture, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(GetTwoLetterIsoCode(language.Culture), requestedCulture, StringComparison.OrdinalIgnoreCase));

            if (matchedLanguage is not null) return matchedLanguage.Culture;
        }

        ApplicationLanguageDto defaultLanguage = await _applicationLanguageProvider.GetDefaultLanguageAsync(cancellationToken);

        return !string.IsNullOrWhiteSpace(defaultLanguage.Culture)
            ? defaultLanguage.Culture
            : activeLanguages.OrderByDescending(language => language.IsDefault).ThenBy(language => language.Name).FirstOrDefault()?.Culture ?? SafeFallbackCulture;
    }

    private static string GetTwoLetterIsoCode(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture)) return string.Empty;

        string normalizedCulture = culture.Trim();
        int separatorIndex = normalizedCulture.IndexOf('-');

        return separatorIndex > 0 ? normalizedCulture[..separatorIndex] : normalizedCulture;
    }

    private static string? GetTranslationField(LocalizedTranslationDto translation, string fieldName)
    {
        if (translation.Fields is null) return null;

        return translation.Fields.TryGetValue(fieldName, out string? value) ? value : null;
    }

    private string GetText(string key, string fallback)
    {
        string value = _localizer.GetStringValue(key);

        return string.IsNullOrWhiteSpace(value) || string.Equals(value, key, StringComparison.OrdinalIgnoreCase)
            ? fallback
            : value;
    }

    private static string GetExceptionMessage(Exception exception)
    {
        return !string.IsNullOrWhiteSpace(exception.Message) ? exception.Message : "İşlem sırasında bir hata oluştu.";
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
