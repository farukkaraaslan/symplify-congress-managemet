namespace Symplify.BackOffice.Domain.Enums;

/// <summary>
/// Provider/recipient delivery state. This is intentionally separate from MailOutboxStatus.
/// MailOutboxStatus describes Symplify -> SMTP transport; MailDeliveryStatus describes provider -> recipient server.
/// </summary>
public enum MailDeliveryStatus
{
    Unknown = 0,
    NotTracked = 1,
    Pending = 10,
    Delivered = 20,
    Delayed = 30,
    Bounced = 40,
    Rejected = 50,
    Complaint = 60,
    RenderingFailed = 70
}
