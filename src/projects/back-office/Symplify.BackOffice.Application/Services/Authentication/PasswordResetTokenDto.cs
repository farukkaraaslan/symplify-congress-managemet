namespace Symplify.BackOffice.Application.Services.Authentication;

public sealed class PasswordResetTokenDto
{
    public Guid? UserId { get; set; }

    public bool TokenGenerated { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}
