namespace Symplify.BackOffice.Application.Services.Authentication;

public sealed class RegisteredAuthorUserDto
{
    public Guid UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string EmailConfirmationToken { get; set; } = string.Empty;
}
