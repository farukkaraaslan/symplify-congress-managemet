namespace Symplify.BackOffice.Domain.Enums;

/// <summary>
/// Normalized Amazon SES event types persisted for audit/history.
/// </summary>
public enum MailDeliveryEventType
{
    Unknown = 0,
    Send = 10,
    Delivery = 20,
    DeliveryDelay = 30,
    Bounce = 40,
    Reject = 50,
    Complaint = 60,
    RenderingFailure = 70
}
