using System.Security.Claims;
using System.Text.Json;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Symplify.BackOffice.Application.Features.BulkEmails.Commands.Queue;
using Symplify.BackOffice.Application.Features.BulkEmails.Dtos;
using Symplify.BackOffice.Application.Features.BulkEmails.Queries.GetComposePage;
using Symplify.BackOffice.Application.Features.BulkEmails.Queries.GetHistory;
using Symplify.BackOffice.Application.Features.BulkEmails.Queries.PreviewContent;
using Symplify.BackOffice.Application.Features.BulkEmails.Queries.PreviewRecipients;
using Symplify.BackOffice.Application.Services.Urls;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.WebUI.Models.BulkEmails;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture?}/bulk-emails")]
public sealed class BulkEmailsController : Controller
{
    private const int MaxRecipientAdjustments = 5000;
    private const int MaxRecipientAdjustmentsJsonLength = 500000;

    private static readonly JsonSerializerOptions RecipientJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IMediator _mediator;
    private readonly IBackOfficeViewLocalizer _localizer;
    private readonly IPublicUrlService _publicUrlService;

    public BulkEmailsController(
        IMediator mediator,
        IBackOfficeViewLocalizer localizer,
        IPublicUrlService publicUrlService)
    {
        _mediator = mediator;
        _localizer = localizer;
        _publicUrlService = publicUrlService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(Guid? congressId, CancellationToken cancellationToken)
    {
        string culture = ResolveCulture();
        GetBulkEmailComposePageResponse page = await LoadComposePageAsync(congressId, culture, cancellationToken);

        return View(new BulkEmailComposeViewModel
        {
            CongressId = page.SelectedCongressId ?? Guid.Empty,
            AudienceType = BulkEmailAudienceType.AllRegistered,
            Culture = culture,
            CongressOptions = BuildCongressOptions(page, page.SelectedCongressId)
        });
    }

    [HttpGet("history")]
    public async Task<IActionResult> History(Guid? congressId, CancellationToken cancellationToken)
    {
        string culture = ResolveCulture();
        GetBulkEmailComposePageResponse page = await LoadComposePageAsync(congressId, culture, cancellationToken);

        return View(new BulkEmailHistoryViewModel
        {
            CongressId = page.SelectedCongressId ?? Guid.Empty,
            Culture = culture,
            CongressOptions = BuildCongressOptions(page, page.SelectedCongressId)
        });
    }

    [HttpPost("queue")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Queue(BulkEmailComposeViewModel model, CancellationToken cancellationToken)
    {
        model.Culture = ResolveCulture(model.Culture);
        ValidateAndLocalizeComposeModel(model);

        if (!TryParseRecipientAdjustments(
                model.ExcludedRecipientEmailsJson,
                model.AdditionalRecipientsJson,
                out IReadOnlyCollection<string> excludedRecipientEmails,
                out IReadOnlyCollection<BulkEmailRecipientDto> additionalRecipients))
        {
            ModelState.AddModelError(
                string.Empty,
                GetText(
                    "BackOffice.BulkEmails.Validation.RecipientSelectionInvalid",
                    "Alıcı listesindeki manuel değişiklikler doğrulanamadı. Listeyi yeniden yükleyip tekrar deneyiniz."));
        }

        if (!ModelState.IsValid)
        {
            await PopulateCongressOptionsAsync(model, cancellationToken);
            return View("Index", model);
        }

        try
        {
            QueueBulkEmailResponse response = await _mediator.Send(new QueueBulkEmailCommand
            {
                CongressId = model.CongressId,
                AudienceType = model.AudienceType,
                Culture = model.Culture,
                Subject = model.Subject.Trim(),
                Title = model.Title.Trim(),
                BodyText = model.BodyText,
                ExcludedRecipientEmails = excludedRecipientEmails,
                AdditionalRecipients = additionalRecipients,
                TrackingBaseUrl = BuildTrackingBaseUrl(),
                CurrentUserId = GetCurrentUserId(),
                IsSuperAdmin = User.IsInRole("SuperAdmin")
            }, cancellationToken);

            TempData["SuccessMessage"] = string.Format(
                GetText("BackOffice.BulkEmails.Success.Queued", "{0} e-posta gönderim kuyruğuna alındı."),
                response.QueuedCount);

            if (response.InvalidEmailCount > 0)
            {
                TempData["WarningMessage"] = string.Format(
                    GetText("BackOffice.BulkEmails.Warning.InvalidEmails", "{0} geçersiz e-posta adresi atlandı."),
                    response.InvalidEmailCount);
            }

            return RedirectToAction(nameof(History), new
            {
                culture = ResolveCulture(),
                congressId = model.CongressId
            });
        }
        catch (BusinessException exception)
        {
            ModelState.AddModelError(string.Empty, GetText(exception.Message, "Toplu e-posta gönderimi başlatılamadı."));
            await PopulateCongressOptionsAsync(model, cancellationToken);
            return View("Index", model);
        }
    }

    [HttpPost("preview-recipients")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GetRecipientPreview(
        Guid congressId,
        BulkEmailAudienceType audienceType,
        int pageIndex = 1,
        int pageSize = 25,
        string? search = null,
        string? excludedRecipientEmailsJson = null,
        string? additionalRecipientsJson = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseRecipientAdjustments(
                excludedRecipientEmailsJson,
                additionalRecipientsJson,
                out IReadOnlyCollection<string> excludedRecipientEmails,
                out IReadOnlyCollection<BulkEmailRecipientDto> additionalRecipients))
        {
            return BadRequest(new
            {
                success = false,
                message = GetText(
                    "BackOffice.BulkEmails.Validation.RecipientSelectionInvalid",
                    "Alıcı listesindeki manuel değişiklikler doğrulanamadı. Listeyi sıfırlayıp tekrar deneyiniz.")
            });
        }

        try
        {
            PreviewBulkEmailRecipientsResponse response = await _mediator.Send(
                new PreviewBulkEmailRecipientsQuery
                {
                    CongressId = congressId,
                    AudienceType = audienceType,
                    CurrentUserId = GetCurrentUserId(),
                    IsSuperAdmin = User.IsInRole("SuperAdmin"),
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    Search = search,
                    ExcludedRecipientEmails = excludedRecipientEmails,
                    AdditionalRecipients = additionalRecipients
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                response.RecipientCount,
                response.FilteredCount,
                response.InvalidEmailCount,
                response.PageIndex,
                response.PageSize,
                response.TotalPages,
                recipients = response.Recipients.Select(recipient => new
                {
                    recipient.Name,
                    recipient.Email,
                    recipient.IsManual
                })
            });
        }
        catch (BusinessException exception)
        {
            return BadRequest(new
            {
                success = false,
                message = GetText(exception.Message, "Alıcılar yüklenemedi.")
            });
        }
    }

    [HttpPost("preview-content")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GetContentPreview(
        Guid congressId,
        string? culture,
        string? subject,
        string? title,
        string? bodyText,
        CancellationToken cancellationToken)
    {
        string resolvedCulture = ResolveCulture(culture);
        string? validationMessage = ValidateContentRequest(subject, title, bodyText);
        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            return BadRequest(new
            {
                success = false,
                message = validationMessage
            });
        }

        try
        {
            PreviewBulkEmailContentResponse response = await _mediator.Send(
                new PreviewBulkEmailContentQuery
                {
                    CongressId = congressId,
                    Culture = resolvedCulture,
                    Subject = subject!.Trim(),
                    Title = title!.Trim(),
                    BodyText = bodyText!.Trim(),
                    CurrentUserId = GetCurrentUserId(),
                    IsSuperAdmin = User.IsInRole("SuperAdmin")
                },
                cancellationToken);

            return Json(new
            {
                success = response.CanSend,
                subject = response.Subject,
                htmlBody = response.HtmlBody,
                unsafeLinks = response.UnsafeLinks,
                warningLinks = response.WarningLinks,
                message = response.CanSend
                    ? null
                    : GetText("BackOffice.BulkEmails.Validation.UnsafeLinksDetected", "İçerikte güvenli olmayan bağlantılar bulundu.")
            });
        }
        catch (BusinessException exception)
        {
            return BadRequest(new
            {
                success = false,
                message = GetText(exception.Message, "E-posta ön izlemesi oluşturulamadı.")
            });
        }
    }

    [HttpGet("history/data")]
    public async Task<IActionResult> GetHistoryData(
        Guid congressId,
        int pageIndex = 1,
        int pageSize = 25,
        MailOutboxStatus? status = null,
        bool? opened = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            GetBulkEmailHistoryResponse response = await _mediator.Send(
                new GetBulkEmailHistoryQuery
                {
                    CongressId = congressId,
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    Status = status,
                    Opened = opened,
                    Search = search,
                    CurrentUserId = GetCurrentUserId(),
                    IsSuperAdmin = User.IsInRole("SuperAdmin")
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                response.TotalCount,
                response.PageIndex,
                response.PageSize,
                response.TotalPages,
                response.PendingCount,
                response.SentCount,
                response.FailedCount,
                response.CancelledCount,
                response.OpenedCount,
                items = response.Items.Select(item => new
                {
                    item.Id,
                    item.BatchId,
                    item.RecipientName,
                    item.RecipientEmail,
                    item.Subject,
                    audienceType = (int)item.AudienceType,
                    status = (int)item.Status,
                    item.AttemptCount,
                    item.CreatedAt,
                    item.SentAt,
                    item.FirstOpenedAt,
                    item.LastOpenedAt,
                    item.OpenCount,
                    item.LastError
                })
            });
        }
        catch (BusinessException exception)
        {
            return BadRequest(new
            {
                success = false,
                message = GetText(exception.Message, "Gönderim geçmişi yüklenemedi.")
            });
        }
    }

    private async Task PopulateCongressOptionsAsync(BulkEmailComposeViewModel model, CancellationToken cancellationToken)
    {
        GetBulkEmailComposePageResponse page = await LoadComposePageAsync(model.CongressId, model.Culture, cancellationToken);
        model.CongressOptions = BuildCongressOptions(page, model.CongressId);
    }

    private Task<GetBulkEmailComposePageResponse> LoadComposePageAsync(
        Guid? selectedCongressId,
        string culture,
        CancellationToken cancellationToken)
    {
        return _mediator.Send(new GetBulkEmailComposePageQuery
        {
            CurrentUserId = GetCurrentUserId(),
            IsSuperAdmin = User.IsInRole("SuperAdmin"),
            Culture = culture,
            SelectedCongressId = selectedCongressId
        }, cancellationToken);
    }

    private static IReadOnlyList<SelectListItem> BuildCongressOptions(
        GetBulkEmailComposePageResponse page,
        Guid? selectedCongressId)
    {
        return page.Congresses
            .Select(option => new SelectListItem
            {
                Value = option.Id.ToString(),
                Text = option.Text,
                Selected = selectedCongressId.HasValue && option.Id == selectedCongressId.Value
            })
            .ToList();
    }

    private static bool TryParseRecipientAdjustments(
        string? excludedRecipientEmailsJson,
        string? additionalRecipientsJson,
        out IReadOnlyCollection<string> excludedRecipientEmails,
        out IReadOnlyCollection<BulkEmailRecipientDto> additionalRecipients)
    {
        excludedRecipientEmails = Array.Empty<string>();
        additionalRecipients = Array.Empty<BulkEmailRecipientDto>();

        string excludedJson = string.IsNullOrWhiteSpace(excludedRecipientEmailsJson)
            ? "[]"
            : excludedRecipientEmailsJson;
        string additionalJson = string.IsNullOrWhiteSpace(additionalRecipientsJson)
            ? "[]"
            : additionalRecipientsJson;

        if (excludedJson.Length > MaxRecipientAdjustmentsJsonLength ||
            additionalJson.Length > MaxRecipientAdjustmentsJsonLength)
        {
            return false;
        }

        try
        {
            List<string> excludedItems = JsonSerializer.Deserialize<List<string>>(
                excludedJson,
                RecipientJsonOptions) ?? new List<string>();

            List<BulkEmailRecipientDto> additionalItems = JsonSerializer.Deserialize<List<BulkEmailRecipientDto>>(
                additionalJson,
                RecipientJsonOptions) ?? new List<BulkEmailRecipientDto>();

            if (excludedItems.Count > MaxRecipientAdjustments ||
                additionalItems.Count > MaxRecipientAdjustments)
            {
                return false;
            }

            excludedRecipientEmails = excludedItems
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Select(email => email.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            additionalRecipients = additionalItems
                .Where(recipient => recipient is not null && !string.IsNullOrWhiteSpace(recipient.Email))
                .Select(recipient => new BulkEmailRecipientDto
                {
                    Email = recipient.Email.Trim(),
                    Name = recipient.Name?.Trim() ?? string.Empty,
                    IsManual = true
                })
                .GroupBy(recipient => recipient.Email, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.Last())
                .ToArray();

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private void ValidateAndLocalizeComposeModel(BulkEmailComposeViewModel model)
    {
        bool isEnglish = model.Culture.StartsWith("en", StringComparison.OrdinalIgnoreCase);

        ModelState.Remove(nameof(BulkEmailComposeViewModel.CongressId));
        if (model.CongressId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(BulkEmailComposeViewModel.CongressId),
                GetText(
                    "BackOffice.BulkEmails.Validation.CongressRequired",
                    isEnglish ? "Congress selection is required." : "Kongre seçimi zorunludur."));
        }

        ModelState.Remove(nameof(BulkEmailComposeViewModel.AudienceType));
        if (!Enum.IsDefined(model.AudienceType))
        {
            ModelState.AddModelError(
                nameof(BulkEmailComposeViewModel.AudienceType),
                GetText(
                    "BackOffice.BulkEmails.Validation.AudienceRequired",
                    isEnglish ? "Select a valid recipient group." : "Geçerli bir alıcı grubu seçiniz."));
        }

        ValidateLocalizedTextField(
            nameof(BulkEmailComposeViewModel.Subject),
            model.Subject,
            200,
            "BackOffice.BulkEmails.Validation.SubjectRequired",
            isEnglish ? "Email subject is required." : "E-posta konusu zorunludur.",
            "BackOffice.BulkEmails.Validation.SubjectTooLong",
            isEnglish
                ? "Email subject can contain at most 200 characters."
                : "E-posta konusu en fazla 200 karakter olabilir.");

        if (!string.IsNullOrWhiteSpace(model.Subject) &&
            (model.Subject.Contains('\r') || model.Subject.Contains('\n')))
        {
            ModelState.AddModelError(
                nameof(BulkEmailComposeViewModel.Subject),
                GetText(
                    "BackOffice.BulkEmails.Validation.SubjectInvalid",
                    isEnglish
                        ? "Email subject cannot contain line breaks."
                        : "E-posta konusu satır sonu içeremez."));
        }

        ValidateLocalizedTextField(
            nameof(BulkEmailComposeViewModel.Title),
            model.Title,
            200,
            "BackOffice.BulkEmails.Validation.TitleRequired",
            isEnglish ? "Email title is required." : "Mail başlığı zorunludur.",
            "BackOffice.BulkEmails.Validation.TitleTooLong",
            isEnglish
                ? "Email title can contain at most 200 characters."
                : "Mail başlığı en fazla 200 karakter olabilir.");

        ValidateLocalizedTextField(
            nameof(BulkEmailComposeViewModel.BodyText),
            model.BodyText,
            20000,
            "BackOffice.BulkEmails.Validation.BodyRequired",
            isEnglish ? "Email content is required." : "Mail içeriği zorunludur.",
            "BackOffice.BulkEmails.Validation.BodyTooLong",
            isEnglish
                ? "Email content can contain at most 20,000 characters."
                : "Mail içeriği en fazla 20.000 karakter olabilir.");

        ModelState.Remove(nameof(BulkEmailComposeViewModel.ExcludedRecipientEmailsJson));
        ModelState.Remove(nameof(BulkEmailComposeViewModel.AdditionalRecipientsJson));

        if ((model.ExcludedRecipientEmailsJson?.Length ?? 0) > MaxRecipientAdjustmentsJsonLength ||
            (model.AdditionalRecipientsJson?.Length ?? 0) > MaxRecipientAdjustmentsJsonLength)
        {
            ModelState.AddModelError(
                string.Empty,
                GetText(
                    "BackOffice.BulkEmails.Validation.RecipientSelectionInvalid",
                    isEnglish
                        ? "Recipient list changes could not be validated. Reload the list and try again."
                        : "Alıcı listesindeki manuel değişiklikler doğrulanamadı. Listeyi yeniden yükleyip tekrar deneyiniz."));
        }
    }

    private void ValidateLocalizedTextField(
        string fieldName,
        string? value,
        int maximumLength,
        string requiredKey,
        string requiredFallback,
        string tooLongKey,
        string tooLongFallback)
    {
        ModelState.Remove(fieldName);

        if (string.IsNullOrWhiteSpace(value))
        {
            ModelState.AddModelError(fieldName, GetText(requiredKey, requiredFallback));
            return;
        }

        if (value.Length > maximumLength)
        {
            ModelState.AddModelError(fieldName, GetText(tooLongKey, tooLongFallback));
        }
    }

    private string? ValidateContentRequest(string? subject, string? title, string? bodyText)
    {
        bool isEnglish = ResolveCulture().StartsWith("en", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(subject))
        {
            return GetText(
                "BackOffice.BulkEmails.Validation.SubjectRequired",
                isEnglish ? "Email subject is required." : "E-posta konusu zorunludur.");
        }

        if (subject.Length > 200)
        {
            return GetText(
                "BackOffice.BulkEmails.Validation.SubjectTooLong",
                isEnglish
                    ? "Email subject can contain at most 200 characters."
                    : "E-posta konusu en fazla 200 karakter olabilir.");
        }

        if (subject.Contains('\r') || subject.Contains('\n'))
        {
            return GetText(
                "BackOffice.BulkEmails.Validation.SubjectInvalid",
                isEnglish
                    ? "Email subject cannot contain line breaks."
                    : "E-posta konusu satır sonu içeremez.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return GetText(
                "BackOffice.BulkEmails.Validation.TitleRequired",
                isEnglish ? "Email title is required." : "Mail başlığı zorunludur.");
        }

        if (title.Length > 200)
        {
            return GetText(
                "BackOffice.BulkEmails.Validation.TitleTooLong",
                isEnglish
                    ? "Email title can contain at most 200 characters."
                    : "Mail başlığı en fazla 200 karakter olabilir.");
        }

        if (string.IsNullOrWhiteSpace(bodyText))
        {
            return GetText(
                "BackOffice.BulkEmails.Validation.BodyRequired",
                isEnglish ? "Email content is required." : "Mail içeriği zorunludur.");
        }

        if (bodyText.Length > 20000)
        {
            return GetText(
                "BackOffice.BulkEmails.Validation.BodyTooLong",
                isEnglish
                    ? "Email content can contain at most 20,000 characters."
                    : "Mail içeriği en fazla 20.000 karakter olabilir.");
        }

        return null;
    }

    private Guid? GetCurrentUserId()
    {
        string? value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out Guid userId) ? userId : null;
    }

    private string BuildTrackingBaseUrl()
    {
        return _publicUrlService.BaseUrl;
    }

    private string ResolveCulture(string? value = null)
    {
        string? routeCulture = RouteData.Values["culture"]?.ToString();
        string culture = string.IsNullOrWhiteSpace(value) ? routeCulture ?? "tr-TR" : value;
        return culture.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "en-US" : "tr-TR";
    }

    private string GetText(string key, string fallback)
    {
        string value = _localizer.GetStringValue(key);
        return string.IsNullOrWhiteSpace(value) || string.Equals(value, key, StringComparison.Ordinal)
            ? fallback
            : value;
    }
}
