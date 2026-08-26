using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Domain.Communication;

/// <summary>
/// Immutable delivery-provider event history for one outgoing mail.
/// The parent MailOutboxMessage keeps the latest summarized state for fast listing.
/// </summary>
public sealed class MailDeliveryEvent : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid MailOutboxMessageId { get; set; }

    /// <summary>
    /// SNS message id. Used as the idempotency key because SNS can retry notifications.
    /// </summary>
    public string ProviderEventId { get; set; } = string.Empty;

    /// <summary>
    /// Amazon SES message id from mail.messageId.
    /// </summary>
    public string? ProviderMessageId { get; set; }

    public MailDeliveryEventType EventType { get; set; }

    public DateTime OccurredAt { get; set; }

    public string? StatusCode { get; set; }

    public string? DiagnosticCode { get; set; }

    public string? BounceType { get; set; }

    public string? BounceSubType { get; set; }

    public string? SmtpResponse { get; set; }

    public string? Detail { get; set; }

    public MailOutboxMessage MailOutboxMessage { get; set; } = null!;
}
