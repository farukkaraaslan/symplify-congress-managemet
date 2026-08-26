namespace Symplify.BackOffice.Domain.Enums;

public enum MailOutboxStatus
{
    Pending = 1,
    Sent = 2,
    Failed = 3,
    Cancelled = 4,

    /// <summary>
    /// Reserved for an immediate, DB-first transport operation such as the organization SMTP test.
    /// The background dispatcher only consumes Pending rows.
    /// </summary>
    Processing = 5
}
