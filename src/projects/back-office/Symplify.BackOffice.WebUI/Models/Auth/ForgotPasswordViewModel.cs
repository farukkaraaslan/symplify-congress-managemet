namespace Symplify.BackOffice.WebUI.Models.Auth;

public sealed class ForgotPasswordViewModel
{
    public string Email { get; set; } = string.Empty;

    public bool MailSent { get; set; }

    public Guid? OrganizationId { get; set; }

    public string? OrganizationSlug { get; set; }

    public string? OrganizationName { get; set; }

    public string? OrganizationShortName { get; set; }

    public string? OrganizationLogoLightUrl { get; set; }

    public string? OrganizationLogoDarkUrl { get; set; }

    public bool HasOrganizationContext => OrganizationId.HasValue && OrganizationId.Value != Guid.Empty;
}
