namespace Symplify.BackOffice.Application.Features.Auth.Commands.Register;

public sealed class RegisteredResponse
{
    public Guid UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string EmailConfirmationToken { get; set; } = string.Empty;

    public Guid OrganizationId { get; set; }

    public string OrganizationName { get; set; } = string.Empty;

    public string OrganizationShortName { get; set; } = string.Empty;

    public string OrganizationSlug { get; set; } = string.Empty;

    public string? OrganizationLogoLightPath { get; set; }
}
