using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Services.Email;

public sealed class BackOfficeEmailMessage
{
    /// <summary>
    /// Correlates this transport attempt with MailOutboxMessages.Id. Required for SES event tracking.
    /// </summary>
    public Guid? TrackingId { get; set; }

    public MailMessageType MailType { get; set; } = MailMessageType.Unknown;

    /// <summary>
    /// Required SMTP scope. Every mail is sent with the configuration of its organization.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Optional business context retained for tracing, reporting and congress-specific links.
    /// SMTP configuration is not resolved from this value.
    /// </summary>
    public Guid? CongressId { get; set; }

    public string? FromEmail { get; set; }

    public string? FromName { get; set; }

    public string? ReplyToEmail { get; set; }

    public string? ReplyToName { get; set; }

    public string ToEmail { get; set; } = string.Empty;

    public string? ToName { get; set; }

    public string Subject { get; set; } = string.Empty;

    public string HtmlBody { get; set; } = string.Empty;

    public string? AttachmentPath { get; set; }

    public string? AttachmentBucketName { get; set; }

    public string? AttachmentObjectName { get; set; }

    public string? AttachmentFileName { get; set; }

    public string? AttachmentContentType { get; set; }
}
