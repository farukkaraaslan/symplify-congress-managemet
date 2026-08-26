namespace Symplify.BackOffice.Infrastructure.Email;

public sealed class BackOfficeMailOptions
{
    public const string SectionName = "Mail";

    public string Provider { get; set; } = "Smtp";

    public string BrandName { get; set; } = "Symplify";

    public string FallbackLogoPath { get; set; } = "/assets/images/logo/symplify-logo-horizontal-light.svg";

    public MailOutboxOptions Outbox { get; set; } = new();

    public SesMailTrackingOptions SesTracking { get; set; } = new();
}

public sealed class MailOutboxOptions
{
    public bool Enabled { get; set; } = true;

    public int IntervalSeconds { get; set; } = 30;

    public int BatchSize { get; set; } = 10;

    public int MaxAttemptCount { get; set; } = 5;
}

public sealed class SesMailTrackingOptions
{
    public bool Enabled { get; set; }

    /// <summary>
    /// SES Configuration Set attached through X-SES-CONFIGURATION-SET.
    /// Must exist in the same AWS account/region as the SMTP credentials.
    /// </summary>
    public string ConfigurationSetName { get; set; } = string.Empty;

    /// <summary>
    /// SNS Topic ARNs accepted by the public webhook. Leave empty only when SES tracking is disabled.
    /// </summary>
    public string[] AllowedTopicArns { get; set; } = Array.Empty<string>();

    public bool VerifySnsSignature { get; set; } = true;

    public bool AutoConfirmSubscription { get; set; } = true;
}
