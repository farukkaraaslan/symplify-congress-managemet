namespace Symplify.BackOffice.Application.Services.Authentication;

public sealed class AuthenticatedUserDto
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public IReadOnlyCollection<string> OperationClaims { get; set; } = Array.Empty<string>();
}
