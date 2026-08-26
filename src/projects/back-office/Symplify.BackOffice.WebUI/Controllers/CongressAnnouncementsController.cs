using Core.Application.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressAnnouncements.Commands.Create;
using Symplify.BackOffice.Application.Features.CongressAnnouncements.Commands.Delete;
using Symplify.BackOffice.Application.Features.CongressAnnouncements.Commands.Reorder;
using Symplify.BackOffice.Application.Features.CongressAnnouncements.Commands.Update;
using Symplify.BackOffice.Application.Features.CongressAnnouncements.Queries.GetForUpdate;
using Symplify.BackOffice.Application.Features.CongressAnnouncements.Queries.GetList;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.WebUI.Models.CongressAnnouncements;
using Symplify.BackOffice.WebUI.Models.Shared.DataTables;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture=tr-TR}/[controller]/[action]")]
public sealed class CongressAnnouncementsController : Controller
{
    private const string SafeFallbackCulture = "tr-TR";

    private const string TitleField = "Title";
    private const string SummaryField = "Summary";
    private const string ContentField = "Content";
    private const string SeoTitleField = "SeoTitle";
    private const string SeoDescriptionField = "SeoDescription";

    private readonly IMediator _mediator;
    private readonly IApplicationLanguageProvider _applicationLanguageProvider;
    private readonly IBackOfficeViewLocalizer _localizer;

    public CongressAnnouncementsController(
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
            allowedSortColumns: new[]
            {
                "order",
                "title",
                "type",
                "status",
                "publishStartDate",
                "publishEndDate",
                "isActive"
            });

        string culture = await ResolveCurrentCultureAsync(cancellationToken);

        var response = await _mediator.Send(
            new GetListCongressAnnouncementQuery
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
                type = item.Type.ToString(),
                typeText = GetAnnouncementTypeText(item.Type),
                status = item.Status.ToString(),
                statusText = GetAnnouncementStatusText(item.Status),
                publishStartDate = FormatDateTime(item.PublishStartDate),
                publishEndDate = FormatDateTime(item.PublishEndDate),
                isPinned = item.IsPinned,
                showOnHomePage = item.ShowOnHomePage,
                showInTicker = item.ShowInTicker,
                externalUrl = item.ExternalUrl,
                attachmentPath = item.AttachmentPath,
                order = item.Order,
                isActive = item.IsActive,
                isCurrentlyPublished = item.IsCurrentlyPublished,
                title = item.Title,
                summary = item.Summary,
                content = item.Content,
                seoTitle = item.SeoTitle,
                seoDescription = item.SeoDescription,
                isFallback = item.IsFallback
            })
        });
    }

    [HttpGet]
    public async Task<IActionResult> CreateModal(Guid congressId, CancellationToken cancellationToken)
    {
        CreateCongressAnnouncementViewModel model = await BuildCreateViewModelAsync(congressId, cancellationToken);
        return PartialView("~/Views/CongressAnnouncements/_CreateAnnouncementModal.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromForm] CreateCongressAnnouncementViewModel model,
        CancellationToken cancellationToken)
    {
        ValidateCreateModel(model);

        if (!ModelState.IsValid)
            return BadRequest(CreateValidationErrorResponse());

        try
        {
            CreatedCongressAnnouncementResponse response = await _mediator.Send(
                new CreateCongressAnnouncementCommand
                {
                    CongressId = model.CongressId,
                    Type = model.Type,
                    Status = model.Status,
                    PublishStartDate = model.PublishStartDate,
                    PublishEndDate = model.PublishEndDate,
                    IsPinned = model.IsPinned,
                    ShowOnHomePage = model.ShowOnHomePage,
                    ShowInTicker = model.ShowInTicker,
                    ExternalUrl = model.ExternalUrl,
                    AttachmentPath = model.AttachmentPath,
                    Order = model.Order,
                    IsActive = model.IsActive,
                    Translations = BuildTranslationInputs(model.Translations)
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                id = response.Id,
                message = GetText("BackOffice.CongressAnnouncements.Messages.Created", "Duyuru başarıyla oluşturuldu.")
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
            new GetCongressAnnouncementForUpdateQuery
            {
                Id = id,
                CongressId = congressId
            },
            cancellationToken);

        UpdateCongressAnnouncementViewModel model = new()
        {
            Id = response.Id,
            CongressId = response.CongressId,
            Type = response.Type,
            Status = response.Status,
            PublishStartDate = response.PublishStartDate,
            PublishEndDate = response.PublishEndDate,
            IsPinned = response.IsPinned,
            ShowOnHomePage = response.ShowOnHomePage,
            ShowInTicker = response.ShowInTicker,
            ExternalUrl = response.ExternalUrl,
            AttachmentPath = response.AttachmentPath,
            Order = response.Order,
            IsActive = response.IsActive,
            Translations = response.Translations.Select(translation => new CongressAnnouncementTranslationViewModel
            {
                LanguageId = translation.LanguageId,
                Culture = translation.Culture,
                LanguageName = translation.LanguageName,
                IsDefault = translation.IsDefault,
                Exists = translation.Exists,
                Title = GetField(translation.Fields, TitleField),
                Summary = GetField(translation.Fields, SummaryField),
                Content = GetField(translation.Fields, ContentField),
                SeoTitle = GetField(translation.Fields, SeoTitleField),
                SeoDescription = GetField(translation.Fields, SeoDescriptionField)
            }).ToList()
        };

        return PartialView("~/Views/CongressAnnouncements/_UpdateAnnouncementModal.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        [FromForm] UpdateCongressAnnouncementViewModel model,
        CancellationToken cancellationToken)
    {
        ValidateUpdateModel(model);

        if (!ModelState.IsValid)
            return BadRequest(CreateValidationErrorResponse());

        try
        {
            await _mediator.Send(
                new UpdateCongressAnnouncementCommand
                {
                    Id = model.Id,
                    CongressId = model.CongressId,
                    Type = model.Type,
                    Status = model.Status,
                    PublishStartDate = model.PublishStartDate,
                    PublishEndDate = model.PublishEndDate,
                    IsPinned = model.IsPinned,
                    ShowOnHomePage = model.ShowOnHomePage,
                    ShowInTicker = model.ShowInTicker,
                    ExternalUrl = model.ExternalUrl,
                    AttachmentPath = model.AttachmentPath,
                    Order = model.Order,
                    IsActive = model.IsActive,
                    Translations = BuildTranslationInputs(model.Translations)
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("BackOffice.CongressAnnouncements.Messages.Updated", "Duyuru başarıyla güncellendi.")
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
    public async Task<IActionResult> Delete(
        [FromForm] Guid id,
        [FromForm] Guid congressId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(
                new DeleteCongressAnnouncementCommand
                {
                    Id = id
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("BackOffice.CongressAnnouncements.Messages.Deleted", "Duyuru başarıyla silindi.")
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
                new ReorderCongressAnnouncementsCommand
                {
                    CongressId = congressId,
                    Items = request.Items
                        .Where(item => item.Id != Guid.Empty)
                        .Select(item => new ReorderCongressAnnouncementItemDto
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
                message = GetText("BackOffice.CongressAnnouncements.Messages.Reordered", "Duyuru sıralaması güncellendi.")
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

    private async Task<CreateCongressAnnouncementViewModel> BuildCreateViewModelAsync(
        Guid congressId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ApplicationLanguageDto> languages = await _applicationLanguageProvider.GetActiveLanguagesAsync(cancellationToken);

        return new CreateCongressAnnouncementViewModel
        {
            CongressId = congressId,
            IsActive = true,
            ShowOnHomePage = true,
            Type = CongressAnnouncementType.General,
            Status = CongressAnnouncementStatus.Draft,
            Translations = languages
                .OrderByDescending(language => language.IsDefault)
                .ThenBy(language => language.Name)
                .Select(language => new CongressAnnouncementTranslationViewModel
                {
                    LanguageId = language.Id,
                    Culture = language.Culture,
                    LanguageName = language.Name,
                    IsDefault = language.IsDefault
                })
                .ToList()
        };
    }

    private void ValidateCreateModel(CreateCongressAnnouncementViewModel model)
    {
        ValidateBaseModel(model.CongressId, model.PublishStartDate, model.PublishEndDate, model.ExternalUrl, model.AttachmentPath, model.Translations);
    }

    private void ValidateUpdateModel(UpdateCongressAnnouncementViewModel model)
    {
        if (model.Id == Guid.Empty)
            ModelState.AddModelError(nameof(model.Id), GetText("Common.InvalidRequest", "Geçersiz istek."));

        ValidateBaseModel(model.CongressId, model.PublishStartDate, model.PublishEndDate, model.ExternalUrl, model.AttachmentPath, model.Translations);
    }

    private void ValidateBaseModel(
        Guid congressId,
        DateTime? publishStartDate,
        DateTime? publishEndDate,
        string? externalUrl,
        string? attachmentPath,
        List<CongressAnnouncementTranslationViewModel> translations)
    {
        if (congressId == Guid.Empty)
            ModelState.AddModelError(nameof(CreateCongressAnnouncementViewModel.CongressId), GetText("BackOffice.CongressAnnouncements.Validation.CongressRequired", "Kongre bilgisi zorunludur."));

        if (publishStartDate.HasValue && publishEndDate.HasValue && publishEndDate.Value < publishStartDate.Value)
            ModelState.AddModelError(nameof(CreateCongressAnnouncementViewModel.PublishEndDate), GetText("BackOffice.CongressAnnouncements.Validation.PublishDateRangeInvalid", "Yayın bitiş tarihi yayın başlangıç tarihinden önce olamaz."));

        if (!string.IsNullOrWhiteSpace(externalUrl) && externalUrl.Length > 1000)
            ModelState.AddModelError(nameof(CreateCongressAnnouncementViewModel.ExternalUrl), GetText("BackOffice.CongressAnnouncements.Validation.ExternalUrlTooLong", "Harici link en fazla 1000 karakter olabilir."));

        if (!string.IsNullOrWhiteSpace(attachmentPath) && attachmentPath.Length > 1000)
            ModelState.AddModelError(nameof(CreateCongressAnnouncementViewModel.AttachmentPath), GetText("BackOffice.CongressAnnouncements.Validation.AttachmentPathTooLong", "Ek dosya yolu en fazla 1000 karakter olabilir."));

        int defaultLanguageIndex = translations.FindIndex(translation => translation.IsDefault);

        if (defaultLanguageIndex < 0)
        {
            ModelState.AddModelError(nameof(CreateCongressAnnouncementViewModel.Translations), GetText("Common.InvalidRequest", "Geçersiz istek."));
            return;
        }

        CongressAnnouncementTranslationViewModel defaultTranslation = translations[defaultLanguageIndex];

        if (string.IsNullOrWhiteSpace(defaultTranslation.Title))
        {
            ModelState.AddModelError(
                $"Translations[{defaultLanguageIndex}].Title",
                GetText("BackOffice.CongressAnnouncements.Validation.TitleRequired", "Varsayılan dilde duyuru başlığı zorunludur."));
        }

        for (int index = 0; index < translations.Count; index++)
        {
            CongressAnnouncementTranslationViewModel translation = translations[index];

            bool hasAnyValue =
                !string.IsNullOrWhiteSpace(translation.Title) ||
                !string.IsNullOrWhiteSpace(translation.Summary) ||
                !string.IsNullOrWhiteSpace(translation.Content) ||
                !string.IsNullOrWhiteSpace(translation.SeoTitle) ||
                !string.IsNullOrWhiteSpace(translation.SeoDescription);

            if (!translation.IsDefault && hasAnyValue && string.IsNullOrWhiteSpace(translation.Title))
            {
                ModelState.AddModelError(
                    $"Translations[{index}].Title",
                    GetText("BackOffice.CongressAnnouncements.Validation.TranslationTitleRequired", "Bu dil için herhangi bir içerik girildiyse duyuru başlığı da zorunludur."));
            }
        }
    }

    private ICollection<TranslationInputDto> BuildTranslationInputs(
        IEnumerable<CongressAnnouncementTranslationViewModel> translations)
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
                    [SummaryField] = NormalizeText(translation.Summary),
                    [ContentField] = NormalizeHtml(translation.Content),
                    [SeoTitleField] = NormalizeText(translation.SeoTitle),
                    [SeoDescriptionField] = NormalizeText(translation.SeoDescription)
                }
            })
            .ToList();
    }

    private static bool HasAnyTranslationValue(CongressAnnouncementTranslationViewModel translation)
    {
        return !string.IsNullOrWhiteSpace(translation.Title) ||
               !string.IsNullOrWhiteSpace(translation.Summary) ||
               !string.IsNullOrWhiteSpace(translation.Content) ||
               !string.IsNullOrWhiteSpace(translation.SeoTitle) ||
               !string.IsNullOrWhiteSpace(translation.SeoDescription);
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

    private async Task<string> NormalizeCultureFromApplicationLanguagesAsync(
        string? culture,
        CancellationToken cancellationToken)
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

    private string GetAnnouncementTypeText(CongressAnnouncementType type)
    {
        return GetText($"BackOffice.CongressAnnouncements.Types.{type}", type.ToString());
    }

    private string GetAnnouncementStatusText(CongressAnnouncementStatus status)
    {
        return GetText($"BackOffice.CongressAnnouncements.Statuses.{status}", status.ToString());
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

    private static string? NormalizeHtml(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? FormatDateTime(DateTime? value)
    {
        return value?.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
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
}
