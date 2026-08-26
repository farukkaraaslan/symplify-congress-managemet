using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Domain.Communication;

/// <summary>
/// Master record for every outgoing BackOffice email.
/// All application mail flows must create this record before transport is attempted.
/// </summary>
public sealed class MailOutboxMessage : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public string ToEmail { get; set; } = string.Empty;

    public string? ToName { get; set; }

    public string Subject { get; set; } = string.Empty;

    public string HtmlBody { get; set; } = string.Empty;

    /// <summary>
    /// Business-purpose classification. Historical rows can remain Unknown.
    /// </summary>
    public MailMessageType MailType { get; set; } = MailMessageType.Unknown;

    /// <summary>
    /// Required SMTP scope. Every queued mail is sent through this organization configuration.
    /// Nullable only for backward compatibility with historical rows.
    /// </summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>
    /// Optional congress context used for reporting, links and business traceability.
    /// SMTP configuration is never resolved from this value.
    /// </summary>
    public Guid? CongressId { get; set; }

    /// <summary>
    /// Optional identity user correlation. ToEmail/ToName are still the immutable recipient snapshot.
    /// </summary>
    public Guid? RelatedUserId { get; set; }

    public Guid? RelatedAuthorId { get; set; }

    /// <summary>
    /// Sender identity snapshot captured when the row is queued. SMTP transport credentials are resolved
    /// from the current organization configuration at dispatch time.
    /// </summary>
    public string? FromEmail { get; set; }

    public string? FromName { get; set; }

    public string? ReplyToEmail { get; set; }

    public string? ReplyToName { get; set; }

    /// <summary>
    /// Legacy local file path or object name. Kept for backward compatibility.
    /// </summary>
    public string? AttachmentPath { get; set; }

    public string? AttachmentBucketName { get; set; }

    public string? AttachmentObjectName { get; set; }

    public string? AttachmentFileName { get; set; }

    public string? AttachmentContentType { get; set; }

    /// <summary>
    /// Symplify -> SMTP transport state. Do not use this property as proof of recipient delivery.
    /// </summary>
    public MailOutboxStatus Status { get; set; } = MailOutboxStatus.Pending;

    public int AttemptCount { get; set; }

    public DateTime? LastAttemptAt { get; set; }

    public DateTime? SentAt { get; set; }

    public string? LastError { get; set; }

    /// <summary>
    /// Provider -> recipient server delivery state. Amazon SES events update this independently from Status.
    /// </summary>
    public MailDeliveryStatus DeliveryStatus { get; set; } = MailDeliveryStatus.Unknown;

    /// <summary>
    /// Transport/provider name, e.g. AmazonSES or SMTP.
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// Amazon SES mail.messageId. SMTP itself does not expose this value through System.Net.Mail;
    /// it is populated from SES event publishing.
    /// </summary>
    public string? ProviderMessageId { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? BouncedAt { get; set; }

    public DateTime? ComplainedAt { get; set; }

    public DateTime? LastDeliveryEventAt { get; set; }

    public string? DeliveryStatusCode { get; set; }

    public string? DeliveryDiagnosticCode { get; set; }

    public string? DeliverySmtpResponse { get; set; }

    public string? BounceType { get; set; }

    public string? BounceSubType { get; set; }

    /// <summary>
    /// When true, HtmlBody is redacted after successful transport or terminal transport failure.
    /// Used for confirmation/reset links that contain security tokens.
    /// </summary>
    public bool ContainsSensitiveContent { get; set; }

    public Guid? RelatedSubmissionId { get; set; }

    public Guid? AcceptanceLetterId { get; set; }

    /// <summary>
    /// Participation certificate associated with this message. Used for link publication/revocation;
    /// certificate emails do not carry PDF attachments.
    /// </summary>
    public Guid? ParticipationCertificateId { get; set; }

    /// <summary>
    /// Groups the recipient rows created by one bulk-email submission.
    /// Null for ordinary system emails.
    /// </summary>
    public Guid? BulkEmailBatchId { get; set; }

    public BulkEmailAudienceType? BulkEmailAudienceType { get; set; }

    public string? BulkEmailCulture { get; set; }

    /// <summary>
    /// Random non-PII token used by the open-tracking pixel.
    /// </summary>
    public Guid? TrackingToken { get; set; }

    public DateTime? FirstOpenedAt { get; set; }

    public DateTime? LastOpenedAt { get; set; }

    public int OpenCount { get; set; }

    public ICollection<MailDeliveryEvent> DeliveryEvents { get; set; } = new HashSet<MailDeliveryEvent>();
}
