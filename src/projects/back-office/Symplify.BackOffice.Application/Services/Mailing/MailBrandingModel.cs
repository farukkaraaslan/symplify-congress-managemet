namespace Symplify.BackOffice.Application.Services.Mailing;

public sealed class MailBrandingModel
{
    public const string OrganizationLogoContentId = "symplify-organization-logo";

    public string BrandName { get; set; } = "Symplify";

    public string? ContextTitle { get; set; }

    /// <summary>
    /// CID used by the mail template. The actual image is loaded server-side from
    /// private object storage by the SMTP sender.
    /// </summary>
    public string? LogoContentId { get; set; }

    public string? LogoAltText { get; set; }
}
