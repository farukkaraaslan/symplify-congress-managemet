namespace Symplify.BackOffice.Application.Services.Authentication;

public sealed class AuthenticatedUserDto
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public Guid? OrganizationId { get; set; }

    public string? OrganizationSlug { get; set; }

    public string? OrganizationName { get; set; }

    public string? OrganizationShortName { get; set; }

    public IReadOnlyCollection<string> OperationClaims { get; set; } = Array.Empty<string>();
}
