namespace Symplify.BackOffice.Application.Features.Auth.Commands.ConfirmEmail;

public sealed class ConfirmEmailResponse
{
    public string Email { get; set; } = string.Empty;

    public bool Success { get; set; }
}
