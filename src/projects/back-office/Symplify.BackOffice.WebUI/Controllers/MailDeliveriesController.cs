using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.Application.Features.MailDeliveries.Queries.GetDetail;
using Symplify.BackOffice.Application.Features.MailDeliveries.Queries.GetList;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.WebUI.Models.Shared.DataTables;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture?}/mail-deliveries")]
public sealed class MailDeliveriesController : Controller
{
    private static readonly string[] AllowedSortColumns =
    [
        "createdDate",
        "mailType",
        "recipient",
        "subject",
        "status",
        "deliveryStatus",
        "sentAt"
    ];

    private readonly IMediator _mediator;
    private readonly IBackOfficeViewLocalizer _localizer;

    public MailDeliveriesController(
        IMediator mediator,
        IBackOfficeViewLocalizer localizer)
    {
        _mediator = mediator;
        _localizer = localizer;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        Guid? organizationId,
        Guid? congressId,
        MailMessageType? mailType,
        MailOutboxStatus? status,
        MailDeliveryStatus? deliveryStatus,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? search,
        CancellationToken cancellationToken = default)
    {
        // İlk render yalnızca filtre seçenekleri ve KPI kartları içindir.
        // Satırlar DataTables tarafından server-side AJAX ile alınır.
        GetMailDeliveryListResponse response = await _mediator.Send(
            BuildQuery(
                organizationId,
                congressId,
                mailType,
                status,
                deliveryStatus,
                dateFrom,
                dateTo,
                search,
                pageIndex: 1,
                pageSize: 10,
                sortColumn: "createdDate",
                sortDirection: "desc"),
            cancellationToken);

        ViewBag.OrganizationId = organizationId;
        ViewBag.CongressId = congressId;
        ViewBag.MailType = mailType;
        ViewBag.Status = status;
        ViewBag.DeliveryStatus = deliveryStatus;
        ViewBag.DateFrom = dateFrom;
        ViewBag.DateTo = dateTo;
        ViewBag.Search = search;

        return View(response);
    }

    [HttpPost("data")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GetList(
        [FromForm] DataTableRequest request,
        [FromForm] Guid? organizationId,
        [FromForm] Guid? congressId,
        [FromForm] MailMessageType? mailType,
        [FromForm] MailOutboxStatus? status,
        [FromForm] MailDeliveryStatus? deliveryStatus,
        [FromForm] DateTime? dateFrom,
        [FromForm] DateTime? dateTo,
        [FromForm] string? search,
        CancellationToken cancellationToken)
    {
        DataTableQueryOptions options = DataTableQueryOptions.From(
            request,
            "createdDate",
            "desc",
            AllowedSortColumns);

        GetMailDeliveryListResponse response = await _mediator.Send(
            BuildQuery(
                organizationId,
                congressId,
                mailType,
                status,
                deliveryStatus,
                dateFrom,
                dateTo,
                string.IsNullOrWhiteSpace(search) ? options.SearchText : search,
                pageIndex: options.Page + 1,
                pageSize: options.PageSize,
                sortColumn: options.SortColumn,
                sortDirection: options.SortDirection),
            cancellationToken);

        return Json(new
        {
            draw = request.Draw,
            recordsTotal = response.RecordsTotalCount,
            recordsFiltered = response.TotalCount,
            summary = new
            {
                total = response.TotalCount,
                pendingTransport = response.PendingTransportCount,
                delivered = response.DeliveredCount,
                bounced = response.BouncedCount,
                failedTransport = response.FailedTransportCount,
                delayed = response.DelayedCount
            },
            data = response.Items.Select(item => new
            {
                id = item.Id,
                createdAt = item.CreatedAt,
                sentAt = item.SentAt,
                deliveredAt = item.DeliveredAt,

                mailType = (int)item.MailType,
                mailTypeName = item.MailType.ToString(),
                mailTypeText = MailTypeText(item.MailType),

                recipientName = item.RecipientName,
                recipientEmail = item.RecipientEmail,

                organizationId = item.OrganizationId,
                organizationName = item.OrganizationName,
                congressId = item.CongressId,
                congressName = item.CongressName,
                submissionNumber = item.SubmissionNumber,

                subject = item.Subject,

                status = (int)item.Status,
                statusName = item.Status.ToString(),
                statusText = TransportText(item.Status),

                deliveryStatus = (int)item.DeliveryStatus,
                deliveryStatusName = item.DeliveryStatus.ToString(),
                deliveryStatusText = DeliveryText(item.DeliveryStatus),

                provider = item.Provider,
                lastError = item.LastError,
                deliveryDiagnosticCode = item.DeliveryDiagnosticCode,
                detailUrl = Url.Action(nameof(Detail), "MailDeliveries", new
                {
                    culture = CurrentCulture(),
                    id = item.Id
                })
            })
        });
    }

    [HttpGet("{id:guid}/detail")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken cancellationToken)
    {
        GetMailDeliveryDetailResponse? response = await _mediator.Send(
            new GetMailDeliveryDetailQuery
            {
                Id = id,
                CurrentUserId = GetCurrentUserId(),
                IsSuperAdmin = User.IsInRole("SuperAdmin")
            },
            cancellationToken);

        return response is null
            ? NotFound()
            : PartialView("_Detail", response);
    }

    private GetMailDeliveryListQuery BuildQuery(
        Guid? organizationId,
        Guid? congressId,
        MailMessageType? mailType,
        MailOutboxStatus? status,
        MailDeliveryStatus? deliveryStatus,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? search,
        int pageIndex,
        int pageSize,
        string sortColumn,
        string sortDirection)
    {
        return new GetMailDeliveryListQuery
        {
            OrganizationId = organizationId,
            CongressId = congressId,
            MailType = mailType,
            Status = status,
            DeliveryStatus = deliveryStatus,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Search = search,
            PageIndex = pageIndex,
            PageSize = pageSize,
            SortColumn = sortColumn,
            SortDirection = sortDirection,
            CurrentUserId = GetCurrentUserId(),
            IsSuperAdmin = User.IsInRole("SuperAdmin")
        };
    }

    private string MailTypeText(MailMessageType value) => value switch
    {
        MailMessageType.EmailConfirmation => T("BackOffice.MailDeliveries.MailType.EmailConfirmation", "E-posta Doğrulama", "Email Confirmation"),
        MailMessageType.PasswordReset => T("BackOffice.MailDeliveries.MailType.PasswordReset", "Şifre Sıfırlama", "Password Reset"),
        MailMessageType.OrganizationMailTest => T("BackOffice.MailDeliveries.MailType.OrganizationMailTest", "Mail Ayarı Testi", "Mail Configuration Test"),
        MailMessageType.SubmissionSentToReview => T("BackOffice.MailDeliveries.MailType.SubmissionSentToReview", "Hakem Süreci", "Sent to Review"),
        MailMessageType.SubmissionPaymentPending => T("BackOffice.MailDeliveries.MailType.SubmissionPaymentPending", "Ödeme Bekleniyor", "Payment Pending"),
        MailMessageType.SubmissionPaymentApproved => T("BackOffice.MailDeliveries.MailType.SubmissionPaymentApproved", "Ödeme Onaylandı", "Payment Approved"),
        MailMessageType.SubmissionAccepted => T("BackOffice.MailDeliveries.MailType.SubmissionAccepted", "Bildiri Kabul", "Submission Accepted"),
        MailMessageType.AcceptanceLetter => T("BackOffice.MailDeliveries.MailType.AcceptanceLetter", "Kabul Belgesi", "Acceptance Letter"),
        MailMessageType.ParticipationCertificate => T("BackOffice.MailDeliveries.MailType.ParticipationCertificate", "Katılım Belgesi", "Participation Certificate"),
        MailMessageType.BulkEmail => T("BackOffice.MailDeliveries.MailType.BulkEmail", "Toplu E-posta", "Bulk Email"),
        MailMessageType.OtherSystem => T("BackOffice.MailDeliveries.MailType.OtherSystem", "Sistem E-postası", "System Email"),
        _ => T("BackOffice.MailDeliveries.MailType.Unknown", "Bilinmiyor", "Unknown")
    };

    private string TransportText(MailOutboxStatus value) => value switch
    {
        MailOutboxStatus.Pending => T("BackOffice.MailDeliveries.Transport.Pending", "Kuyrukta", "Queued"),
        MailOutboxStatus.Sent => T("BackOffice.MailDeliveries.Transport.Sent", "Gönderildi", "Sent"),
        MailOutboxStatus.Failed => T("BackOffice.MailDeliveries.Transport.Failed", "Başarısız", "Failed"),
        MailOutboxStatus.Cancelled => T("BackOffice.MailDeliveries.Transport.Cancelled", "İptal", "Cancelled"),
        MailOutboxStatus.Processing => T("BackOffice.MailDeliveries.Transport.Processing", "Gönderiliyor", "Processing"),
        _ => value.ToString()
    };

    private string DeliveryText(MailDeliveryStatus value) => value switch
    {
        MailDeliveryStatus.Unknown => T("BackOffice.MailDeliveries.Delivery.Unknown", "Bilinmiyor", "Unknown"),
        MailDeliveryStatus.NotTracked => T("BackOffice.MailDeliveries.Delivery.NotTracked", "Takip Edilmiyor", "Not Tracked"),
        MailDeliveryStatus.Pending => T("BackOffice.MailDeliveries.Delivery.Pending", "SES Bekleniyor", "Awaiting SES"),
        MailDeliveryStatus.Delivered => T("BackOffice.MailDeliveries.Delivery.Delivered", "Teslim Edildi", "Delivered"),
        MailDeliveryStatus.Delayed => T("BackOffice.MailDeliveries.Delivery.Delayed", "Gecikiyor", "Delayed"),
        MailDeliveryStatus.Bounced => T("BackOffice.MailDeliveries.Delivery.Bounced", "Bounce", "Bounced"),
        MailDeliveryStatus.Rejected => T("BackOffice.MailDeliveries.Delivery.Rejected", "Reddedildi", "Rejected"),
        MailDeliveryStatus.Complaint => T("BackOffice.MailDeliveries.Delivery.Complaint", "Şikayet", "Complaint"),
        MailDeliveryStatus.RenderingFailed => T("BackOffice.MailDeliveries.Delivery.RenderingFailed", "İçerik Hatası", "Rendering Failed"),
        _ => value.ToString()
    };

    private string T(string key, string tr, string en)
    {
        string value = _localizer.GetStringValue(key);
        if (!string.IsNullOrWhiteSpace(value) && !string.Equals(value, key, StringComparison.Ordinal))
            return value;

        return CurrentCulture().StartsWith("en", StringComparison.OrdinalIgnoreCase)
            ? en
            : tr;
    }

    private string CurrentCulture()
        => RouteData.Values["culture"]?.ToString() ?? "tr-TR";

    private Guid? GetCurrentUserId()
    {
        string? value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out Guid userId)
            ? userId
            : null;
    }
}
