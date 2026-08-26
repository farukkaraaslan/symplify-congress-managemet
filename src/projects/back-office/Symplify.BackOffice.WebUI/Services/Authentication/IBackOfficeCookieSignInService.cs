using Symplify.BackOffice.Application.Services.Authentication;

namespace Symplify.BackOffice.WebUI.Services.Authentication;

public interface IBackOfficeCookieSignInService
{
    Task SignInAsync(
        HttpContext httpContext,
        AuthenticatedUserDto user,
        bool rememberMe,
        CancellationToken cancellationToken = default);

    Task SignOutAsync(HttpContext httpContext);
}