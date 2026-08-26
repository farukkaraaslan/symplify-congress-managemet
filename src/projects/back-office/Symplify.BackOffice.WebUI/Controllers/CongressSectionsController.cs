using Core.Application.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressSections.Commands.Create;
using Symplify.BackOffice.Application.Features.CongressSections.Commands.Delete;
using Symplify.BackOffice.Application.Features.CongressSections.Commands.Reorder;
using Symplify.BackOffice.Application.Features.CongressSections.Commands.Update;
using Symplify.BackOffice.Application.Features.CongressSections.Queries.GetForUpdate;
using Symplify.BackOffice.Application.Features.CongressSections.Queries.GetList;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.WebUI.Models.CongressSections;
using Symplify.BackOffice.WebUI.Models.Shared.DataTables;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture=tr-TR}/[controller]/[action]")]
public sealed class CongressSectionsController : Controller
{
    private const string SafeFallbackCulture = "tr-TR";
    private const string TitleField = "Title";
    private const string ContentField = "Content";

    private static readonly string[] TranslationFieldNames =
    {
        TitleField,
        ContentField
    };

    private readonly IMediator _mediator;
    private readonly IApplicationLanguageProvider _applicationLanguageProvider;
    private readonly IBackOfficeViewLocalizer _localizer;

    public CongressSectionsController(
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
            allowedSortColumns: new[] { "order", "bindingKey", "title", "isActive" });

        string culture = await ResolveCurrentCultureAsync(cancellationToken);

        var response = await _mediator.Send(
            new GetListCongressSectionQuery
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
                bindingKey = item.BindingKey,
                order = item.Order,
                title = item.Title,
                content = item.Content,
                culture,
                isActive = item.IsActive,
                isFallback = item.IsFallback
            })
        });
    }

    [HttpGet]
    public async Task<IActionResult> CreateModal(Guid congressId, CancellationToken cancellationToken)
    {
        CreateCongressSectionViewModel model = await BuildCreateViewModelAsync(congressId, cancellationToken);
        return PartialView("~/Views/CongressSections/_CreateSectionModal.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] CreateCongressSectionViewModel model, CancellationToken cancellationToken)
    {
        ValidateCreateModel(model);

        if (!ModelState.IsValid)
            return BadRequest(CreateValidationErrorResponse());

        try
        {
            CreatedCongressSectionResponse response = await _mediator.Send(
                new CreateCongressSectionCommand
                {
                    CongressId = model.CongressId,
                    BindingKey = NormalizeText(model.BindingKey) ?? string.Empty,
                    Order = model.Order,
                    IsActive = model.IsActive,
                    Translations = BuildTranslationInputs(model.Translations)
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                id = response.Id,
                message = GetText("BackOffice.CongressSections.Messages.Created", "Bölüm başarıyla oluşturuldu.")
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
            new GetCongressSectionForUpdateQuery
            {
                Id = id,
                CongressId = congressId
            },
            cancellationToken);

        UpdateCongressSectionViewModel model = new()
        {
            Id = response.Id,
            CongressId = response.CongressId,
            BindingKey = response.BindingKey,
            Order = response.Order,
            IsActive = response.IsActive,
            Translations = response.Translations.Select(translation => new CongressSectionTranslationViewModel
            {
                LanguageId = translation.LanguageId,
                Culture = translation.Culture,
                LanguageName = translation.LanguageName,
                IsDefault = translation.IsDefault,
                Exists = translation.Exists,
                Title = GetField(translation.Fields, TitleField),
                Content = GetField(translation.Fields, ContentField)
            }).ToList()
        };

        return PartialView("~/Views/CongressSections/_UpdateSectionModal.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromForm] UpdateCongressSectionViewModel model, CancellationToken cancellationToken)
    {
        ValidateUpdateModel(model);

        if (!ModelState.IsValid)
            return BadRequest(CreateValidationErrorResponse());

        try
        {
            await _mediator.Send(
                new UpdateCongressSectionCommand
                {
                    Id = model.Id,
                    CongressId = model.CongressId,
                    BindingKey = NormalizeText(model.BindingKey) ?? string.Empty,
                    Order = model.Order,
                    IsActive = model.IsActive,
                    Translations = BuildTranslationInputs(model.Translations)
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("BackOffice.CongressSections.Messages.Updated", "Bölüm başarıyla güncellendi.")
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
                    new GetCongressSectionForUpdateQuery
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

            await _mediator.Send(new DeleteCongressSectionCommand { Id = id }, cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("BackOffice.CongressSections.Messages.Deleted", "Bölüm başarıyla silindi.")
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
                new ReorderCongressSectionCommand
                {
                    CongressId = congressId,
                    Items = request.Items
                        .Where(item => item.Id != Guid.Empty)
                        .Select(item => new ReorderCongressSectionItemDto
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
                message = GetText("BackOffice.CongressSections.Messages.Reordered", "Bölüm sıralaması güncellendi.")
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

    private async Task<CreateCongressSectionViewModel> BuildCreateViewModelAsync(Guid congressId, CancellationToken cancellationToken)
    {
        IReadOnlyList<ApplicationLanguageDto> languages = await _applicationLanguageProvider.GetActiveLanguagesAsync(cancellationToken);
        int nextOrder = 1;

        if (congressId != Guid.Empty)
        {
            var list = await _mediator.Send(
                new GetListCongressSectionQuery
                {
                    CongressId = congressId,
                    PageRequest = new PageRequest
                    {
                        Page = 0,
                        PageSize = 200
                    }
                },
                cancellationToken);

            nextOrder = list.Count + 1;
        }

        return new CreateCongressSectionViewModel
        {
            CongressId = congressId,
            Order = nextOrder,
            IsActive = true,
            Translations = languages
                .OrderByDescending(language => language.IsDefault)
                .ThenBy(language => language.Name)
                .Select(language => new CongressSectionTranslationViewModel
                {
                    LanguageId = language.Id,
                    Culture = language.Culture,
                    LanguageName = language.Name,
                    IsDefault = language.IsDefault
                })
                .ToList()
        };
    }

    private void ValidateCreateModel(CreateCongressSectionViewModel model)
    {
        ValidateBaseModel(model.CongressId, model.BindingKey, model.Order, model.Translations);
    }

    private void ValidateUpdateModel(UpdateCongressSectionViewModel model)
    {
        if (model.Id == Guid.Empty)
            ModelState.AddModelError(nameof(model.Id), GetText("Common.InvalidRequest", "Geçersiz istek."));

        ValidateBaseModel(model.CongressId, model.BindingKey, model.Order, model.Translations);
    }

    private void ValidateBaseModel(
        Guid congressId,
        string? bindingKey,
        int order,
        List<CongressSectionTranslationViewModel> translations)
    {
        if (congressId == Guid.Empty)
            ModelState.AddModelError(nameof(CreateCongressSectionViewModel.CongressId), GetText("BackOffice.CongressSections.Validation.CongressRequired", "Kongre bilgisi zorunludur."));

        if (string.IsNullOrWhiteSpace(bindingKey))
        {
            ModelState.AddModelError(nameof(CreateCongressSectionViewModel.BindingKey), GetText("BackOffice.CongressSections.Validation.BindingKeyRequired", "Bağlantı anahtarı zorunludur."));
        }
        else if (bindingKey.Trim().Length > 100)
        {
            ModelState.AddModelError(nameof(CreateCongressSectionViewModel.BindingKey), GetText("BackOffice.CongressSections.Validation.BindingKeyTooLong", "Bağlantı anahtarı en fazla 100 karakter olabilir."));
        }

        if (order < 0)
            ModelState.AddModelError(nameof(CreateCongressSectionViewModel.Order), GetText("BackOffice.CongressSections.Validation.OrderInvalid", "Sıralama değeri sıfırdan küçük olamaz."));

        int defaultLanguageIndex = translations.FindIndex(translation => translation.IsDefault);

        if (defaultLanguageIndex < 0)
        {
            ModelState.AddModelError(nameof(CreateCongressSectionViewModel.Translations), GetText("Common.InvalidRequest", "Geçersiz istek."));
            return;
        }

        CongressSectionTranslationViewModel defaultTranslation = translations[defaultLanguageIndex];

        if (string.IsNullOrWhiteSpace(defaultTranslation.Title))
        {
            ModelState.AddModelError(
                $"Translations[{defaultLanguageIndex}].Title",
                GetText("BackOffice.CongressSections.Validation.TitleRequired", "Varsayılan dilde bölüm başlığı zorunludur."));
        }

        for (int index = 0; index < translations.Count; index++)
        {
            CongressSectionTranslationViewModel translation = translations[index];

            bool hasAnyValue =
                !string.IsNullOrWhiteSpace(translation.Title) ||
                !string.IsNullOrWhiteSpace(translation.Content);

            if (!translation.IsDefault && hasAnyValue && string.IsNullOrWhiteSpace(translation.Title))
            {
                ModelState.AddModelError(
                    $"Translations[{index}].Title",
                    GetText("BackOffice.CongressSections.Validation.TranslationTitleRequired", "Bu dil için herhangi bir içerik girildiyse bölüm başlığı da zorunludur."));
            }
        }
    }

    private ICollection<TranslationInputDto> BuildTranslationInputs(IEnumerable<CongressSectionTranslationViewModel> translations)
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
                    [ContentField] = NormalizeHtml(translation.Content)
                }
            })
            .ToList();
    }

    private static bool HasAnyTranslationValue(CongressSectionTranslationViewModel translation)
    {
        return !string.IsNullOrWhiteSpace(translation.Title) ||
               !string.IsNullOrWhiteSpace(translation.Content);
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
        if (string.IsNullOrWhiteSpace(exception.Message))
            return GetText("Common.GenericError", string.Empty);

        return GetText(exception.Message, exception.Message);
    }

    private static string? GetField(IReadOnlyDictionary<string, string?> fields, string key)
    {
        return fields.TryGetValue(key, out string? value) ? value : null;
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeHtml(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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
                    item => item.Value!.Errors
                        .Select(error => GetText(string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Common.InvalidRequest" : error.ErrorMessage, error.ErrorMessage))
                        .ToArray())
        };
    }
}
