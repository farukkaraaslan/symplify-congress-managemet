using Core.Application.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.TransactionStatuses.Queries.GetList;
using Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Commands.Create;
using Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Commands.Delete;
using Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Commands.Update;
using Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Queries.GetForUpdate;
using Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Queries.GetList;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.WebUI.Models.Shared.DataTables;
using Symplify.BackOffice.WebUI.Models.TransactionStatusTransitions;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture=tr-TR}/[controller]/[action]")]
public sealed class TransactionStatusTransitionsController : Controller
{
    private const string SafeFallbackCulture = "tr-TR";
    private const string NameField = "Name";
    private const string DescriptionField = "Description";

    private readonly IMediator _mediator;
    private readonly IApplicationLanguageProvider _applicationLanguageProvider;
    private readonly IBackOfficeViewLocalizer _localizer;

    public TransactionStatusTransitionsController(
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
        TransactionStatusTransitionsIndexViewModel model = await BuildIndexViewModelAsync(cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GetList(
        [FromForm] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        string culture = await ResolveCurrentCultureAsync(cancellationToken);

        DataTableQueryOptions tableOptions = DataTableQueryOptions.From(
            request,
            defaultSortColumn: "fromStatus",
            defaultSortDirection: "asc",
            allowedSortColumns: new[] { "fromStatus", "toStatus", "name", "isActive" });

        var response = await _mediator.Send(
            new GetListTransactionStatusTransitionQuery
            {
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

        List<object> pageItems = response.Items
            .Select((item, index) => new
            {
                rowNumber = tableOptions.Start + index + 1,
                id = item.Id,
                fromStatusId = item.FromStatusId,
                fromStatusCode = item.FromStatusCode,
                fromStatusName = item.FromStatusName,
                toStatusId = item.ToStatusId,
                toStatusCode = item.ToStatusCode,
                toStatusName = item.ToStatusName,
                name = item.Name,
                description = item.Description,
                isActive = item.IsActive
            })
            .Cast<object>()
            .ToList();

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
            return BadRequest(new
            {
                success = false,
                message = GetText("Common.InvalidRequest", "Geçersiz istek.")
            });
        }

        try
        {
            GetTransactionStatusTransitionForUpdateResponse response = await _mediator.Send(
                new GetTransactionStatusTransitionForUpdateQuery
                {
                    Id = id
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                id = response.Id,
                fromStatusId = response.FromStatusId,
                toStatusId = response.ToStatusId,
                isActive = response.IsActive,
                translations = response.Translations
                    .Select(translation => new
                    {
                        languageId = translation.LanguageId,
                        culture = translation.Culture,
                        languageName = translation.LanguageName,
                        isDefault = translation.IsDefault,
                        exists = translation.Exists,
                        name = GetTranslationField(translation, NameField),
                        description = GetTranslationField(translation, DescriptionField)
                    })
                    .ToList()
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
    public async Task<IActionResult> Create(
        [FromForm] CreateTransactionStatusTransitionViewModel model,
        CancellationToken cancellationToken)
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
            CreateTransactionStatusTransitionCommand command = new()
            {
                FromStatusId = model.FromStatusId,
                ToStatusId = model.ToStatusId,
                IsActive = model.IsActive,
                Translations = BuildTranslationInputs(model.Translations)
            };

            await _mediator.Send(command, cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("BackOffice.TransactionStatusTransitions.Messages.Created", "Geçiş oluşturuldu.")
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
    public async Task<IActionResult> Update(
        [FromForm] UpdateTransactionStatusTransitionViewModel model,
        CancellationToken cancellationToken)
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
            UpdateTransactionStatusTransitionCommand command = new()
            {
                Id = model.Id,
                FromStatusId = model.FromStatusId,
                ToStatusId = model.ToStatusId,
                IsActive = model.IsActive,
                Translations = BuildTranslationInputs(model.Translations)
            };

            await _mediator.Send(command, cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("BackOffice.TransactionStatusTransitions.Messages.Updated", "Geçiş güncellendi.")
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
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
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
                new DeleteTransactionStatusTransitionCommand
                {
                    Id = id
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("BackOffice.TransactionStatusTransitions.Messages.Deleted", "Geçiş silindi.")
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

    private async Task<TransactionStatusTransitionsIndexViewModel> BuildIndexViewModelAsync(CancellationToken cancellationToken)
    {
        string culture = await ResolveCurrentCultureAsync(cancellationToken);

        IReadOnlyList<ApplicationLanguageDto> languages = await _applicationLanguageProvider
            .GetActiveLanguagesAsync(cancellationToken);

        List<ApplicationLanguageDto> orderedLanguages = languages
            .OrderByDescending(language => language.IsDefault)
            .ThenBy(language => language.Name)
            .ToList();

        IReadOnlyList<TransactionStatusSelectItemViewModel> statusOptions = await GetStatusOptionsAsync(culture, cancellationToken);

        return new TransactionStatusTransitionsIndexViewModel
        {
            StatusOptions = statusOptions,
            CreateTransactionStatusTransition = new CreateTransactionStatusTransitionViewModel
            {
                IsActive = true,
                Translations = orderedLanguages.Select(language => new CreateTransactionStatusTransitionTranslationViewModel
                {
                    LanguageId = language.Id,
                    Culture = language.Culture,
                    LanguageName = language.Name,
                    IsDefault = language.IsDefault
                }).ToList()
            },
            UpdateTransactionStatusTransition = new UpdateTransactionStatusTransitionViewModel
            {
                IsActive = true,
                Translations = orderedLanguages.Select(language => new UpdateTransactionStatusTransitionTranslationViewModel
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

    private async Task<IReadOnlyList<TransactionStatusSelectItemViewModel>> GetStatusOptionsAsync(
        string culture,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(
            new GetListTransactionStatusQuery
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

        return response.Items
            .OrderBy(item => item.TransactionStatusPhaseName)
            .ThenBy(item => item.Order)
            .ThenBy(item => item.Name)
            .Select(item => new TransactionStatusSelectItemViewModel
            {
                Id = item.Id,
                TransactionStatusPhaseId = item.TransactionStatusPhaseId,
                TransactionStatusPhaseCode = item.TransactionStatusPhaseCode,
                TransactionStatusPhaseName = item.TransactionStatusPhaseName,
                Code = item.Code,
                Name = item.Name
            })
            .ToList();
    }

    private void ValidateCreateModel(CreateTransactionStatusTransitionViewModel model)
    {
        if (model.FromStatusId <= 0)
        {
            ModelState.AddModelError(
                nameof(model.FromStatusId),
                GetText("BackOffice.TransactionStatusTransitions.Validation.FromStatusRequired", "Kaynak durum seçimi zorunludur."));
        }

        if (model.ToStatusId <= 0)
        {
            ModelState.AddModelError(
                nameof(model.ToStatusId),
                GetText("BackOffice.TransactionStatusTransitions.Validation.ToStatusRequired", "Hedef durum seçimi zorunludur."));
        }

        if (model.FromStatusId > 0 && model.ToStatusId > 0 && model.FromStatusId == model.ToStatusId)
        {
            ModelState.AddModelError(
                nameof(model.ToStatusId),
                GetText("BackOffice.TransactionStatusTransitions.Validation.FromAndToStatusCannotBeSame", "Kaynak ve hedef durum aynı olamaz."));
        }

        ValidateDefaultTranslation(model.Translations);
    }

    private void ValidateUpdateModel(UpdateTransactionStatusTransitionViewModel model)
    {
        if (model.Id <= 0)
        {
            ModelState.AddModelError(
                nameof(model.Id),
                GetText("Common.InvalidRequest", "Geçersiz istek."));
        }

        if (model.FromStatusId <= 0)
        {
            ModelState.AddModelError(
                nameof(model.FromStatusId),
                GetText("BackOffice.TransactionStatusTransitions.Validation.FromStatusRequired", "Kaynak durum seçimi zorunludur."));
        }

        if (model.ToStatusId <= 0)
        {
            ModelState.AddModelError(
                nameof(model.ToStatusId),
                GetText("BackOffice.TransactionStatusTransitions.Validation.ToStatusRequired", "Hedef durum seçimi zorunludur."));
        }

        if (model.FromStatusId > 0 && model.ToStatusId > 0 && model.FromStatusId == model.ToStatusId)
        {
            ModelState.AddModelError(
                nameof(model.ToStatusId),
                GetText("BackOffice.TransactionStatusTransitions.Validation.FromAndToStatusCannotBeSame", "Kaynak ve hedef durum aynı olamaz."));
        }

        ValidateDefaultTranslation(model.Translations);
    }

    private void ValidateDefaultTranslation(IReadOnlyList<CreateTransactionStatusTransitionTranslationViewModel> translations)
    {
        int defaultLanguageIndex = translations.ToList().FindIndex(translation => translation.IsDefault);

        if (defaultLanguageIndex < 0)
        {
            ModelState.AddModelError(
                nameof(translations),
                GetText("Common.InvalidRequest", "Geçersiz istek."));
            return;
        }

        if (string.IsNullOrWhiteSpace(translations[defaultLanguageIndex].Name))
        {
            ModelState.AddModelError(
                $"Translations[{defaultLanguageIndex}].Name",
                GetText("BackOffice.TransactionStatusTransitions.Validation.NameRequired", "Ad alanı zorunludur."));
        }
    }

    private void ValidateDefaultTranslation(IReadOnlyList<UpdateTransactionStatusTransitionTranslationViewModel> translations)
    {
        int defaultLanguageIndex = translations.ToList().FindIndex(translation => translation.IsDefault);

        if (defaultLanguageIndex < 0)
        {
            ModelState.AddModelError(
                nameof(translations),
                GetText("Common.InvalidRequest", "Geçersiz istek."));
            return;
        }

        if (string.IsNullOrWhiteSpace(translations[defaultLanguageIndex].Name))
        {
            ModelState.AddModelError(
                $"Translations[{defaultLanguageIndex}].Name",
                GetText("BackOffice.TransactionStatusTransitions.Validation.NameRequired", "Ad alanı zorunludur."));
        }
    }

    private ICollection<TranslationInputDto> BuildTranslationInputs(
        IReadOnlyCollection<CreateTransactionStatusTransitionTranslationViewModel> translations)
    {
        return translations
            .GroupBy(translation => translation.LanguageId)
            .Select(group => group.First())
            .Where(HasAnyTranslationValue)
            .Select(translation => new TranslationInputDto
            {
                LanguageId = translation.LanguageId,
                Fields = BuildTranslationFields(translation)
            })
            .ToList();
    }

    private ICollection<TranslationInputDto> BuildTranslationInputs(
        IReadOnlyCollection<UpdateTransactionStatusTransitionTranslationViewModel> translations)
    {
        return translations
            .GroupBy(translation => translation.LanguageId)
            .Select(group => group.First())
            .Where(HasAnyTranslationValue)
            .Select(translation => new TranslationInputDto
            {
                LanguageId = translation.LanguageId,
                Fields = BuildTranslationFields(translation)
            })
            .ToList();
    }

    private static Dictionary<string, string?> BuildTranslationFields(
        CreateTransactionStatusTransitionTranslationViewModel translation)
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [NameField] = NormalizeText(translation.Name),
            [DescriptionField] = NormalizeText(translation.Description)
        };
    }

    private static Dictionary<string, string?> BuildTranslationFields(
        UpdateTransactionStatusTransitionTranslationViewModel translation)
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [NameField] = NormalizeText(translation.Name),
            [DescriptionField] = NormalizeText(translation.Description)
        };
    }

    private static bool HasAnyTranslationValue(CreateTransactionStatusTransitionTranslationViewModel translation)
    {
        return !string.IsNullOrWhiteSpace(translation.Name) ||
               !string.IsNullOrWhiteSpace(translation.Description);
    }

    private static bool HasAnyTranslationValue(UpdateTransactionStatusTransitionTranslationViewModel translation)
    {
        return !string.IsNullOrWhiteSpace(translation.Name) ||
               !string.IsNullOrWhiteSpace(translation.Description);
    }

    private Dictionary<string, string[]> GetModelStateErrors()
    {
        return ModelState
            .Where(item => item.Value is not null && item.Value.Errors.Count > 0)
            .ToDictionary(
                item => item.Key,
                item => item.Value!.Errors.Select(error => error.ErrorMessage).ToArray());
    }

    private async Task<string> ResolveCurrentCultureAsync(CancellationToken cancellationToken)
    {
        string? formCulture = Request.HasFormContentType
            ? Request.Form["culture"].FirstOrDefault()
            : null;

        if (!string.IsNullOrWhiteSpace(formCulture))
            return await NormalizeCultureFromApplicationLanguagesAsync(formCulture, cancellationToken);

        string? queryCulture = Request.Query["culture"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(queryCulture))
            return await NormalizeCultureFromApplicationLanguagesAsync(queryCulture, cancellationToken);

        string? headerCulture = Request.Headers["X-Culture"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(headerCulture))
            return await NormalizeCultureFromApplicationLanguagesAsync(headerCulture, cancellationToken);

        string? routeCulture = RouteData.Values["culture"]?.ToString();

        if (!string.IsNullOrWhiteSpace(routeCulture))
            return await NormalizeCultureFromApplicationLanguagesAsync(routeCulture, cancellationToken);

        string? pathCulture = HttpContext.Request.Path.Value?
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        return await NormalizeCultureFromApplicationLanguagesAsync(pathCulture, cancellationToken);
    }

    private async Task<string> NormalizeCultureFromApplicationLanguagesAsync(
        string? culture,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _applicationLanguageProvider
            .GetActiveLanguagesAsync(cancellationToken);

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

        ApplicationLanguageDto defaultLanguage = await _applicationLanguageProvider
            .GetDefaultLanguageAsync(cancellationToken);

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

        return separatorIndex > 0
            ? normalizedCulture[..separatorIndex]
            : normalizedCulture;
    }

    private static string? GetTranslationField(LocalizedTranslationDto translation, string fieldName)
    {
        if (translation.Fields is null)
            return null;

        return translation.Fields.TryGetValue(fieldName, out string? value)
            ? value
            : null;
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
        if (!string.IsNullOrWhiteSpace(exception.Message))
            return exception.Message;

        return "İşlem sırasında bir hata oluştu.";
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
