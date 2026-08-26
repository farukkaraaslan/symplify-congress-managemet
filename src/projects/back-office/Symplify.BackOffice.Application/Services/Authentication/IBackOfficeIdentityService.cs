namespace Symplify.BackOffice.Application.Services.Authentication;

public interface IBackOfficeIdentityService
{
    Task<AuthenticatedUserDto> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<RegisteredAuthorUserDto> RegisterAuthorAsync(
        RegisterAuthorUserRequest request,
        CancellationToken cancellationToken = default);

    Task<PasswordResetTokenDto> GeneratePasswordResetTokenAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default);

    Task ConfirmEmailAsync(
        string email,
        string token,
        CancellationToken cancellationToken = default);
}
