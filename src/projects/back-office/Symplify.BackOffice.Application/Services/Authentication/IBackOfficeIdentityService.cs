namespace Symplify.BackOffice.Application.Services.Authentication;

public interface IBackOfficeIdentityService
{
    Task<AuthenticatedUserDto> AuthenticateAsync(
      string email,
      string password,
      CancellationToken cancellationToken = default);
}
