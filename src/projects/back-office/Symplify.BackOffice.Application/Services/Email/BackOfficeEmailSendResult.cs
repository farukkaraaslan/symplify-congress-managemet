namespace Symplify.BackOffice.Application.Services.Email;

public sealed class BackOfficeEmailSendResult
{
    public string Provider { get; init; } = "SMTP";

    /// <summary>
    /// True when the selected SMTP host is Amazon SES and SES event publishing headers were attached.
    /// </summary>
    public bool DeliveryTrackingEnabled { get; init; }
}
