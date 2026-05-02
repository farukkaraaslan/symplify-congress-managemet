using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Symplify.BackOffice.Application.Services.Authentication;
using SecurityClaim = System.Security.Claims.Claim;
using SecurityClaimTypes = System.Security.Claims.ClaimTypes;
using SecurityClaimsIdentity = System.Security.Claims.ClaimsIdentity;
using SecurityClaimsPrincipal = System.Security.Claims.ClaimsPrincipal;

namespace Symplify.BackOffice.WebUI.Services.Authentication;

public sealed class BackOfficeCookieSignInService : IBackOfficeCookieSignInService
{
    public async Task SignInAsync(
        HttpContext httpContext,
        AuthenticatedUserDto user,
        bool rememberMe,
        CancellationToken cancellationToken = default)
    {
        string displayName = !string.IsNullOrWhiteSpace(user.DisplayName)
            ? user.DisplayName
            : user.Email;

        List<SecurityClaim> claims =
        [
            new SecurityClaim(SecurityClaimTypes.NameIdentifier, user.Id.ToString()),
            new SecurityClaim(SecurityClaimTypes.Name, displayName),
            new SecurityClaim(SecurityClaimTypes.Email, user.Email)
        ];

        SecurityClaimsIdentity identity = new(
            claims,
            IdentityConstants.ApplicationScheme,
            SecurityClaimTypes.Name,
            SecurityClaimTypes.Role);

        SecurityClaimsPrincipal principal = new(identity);

        AuthenticationProperties authenticationProperties = new()
        {
            IsPersistent = rememberMe,
            AllowRefresh = true,
            IssuedUtc = DateTimeOffset.UtcNow,
            ExpiresUtc = rememberMe
                ? DateTimeOffset.UtcNow.AddDays(14)
                : DateTimeOffset.UtcNow.AddHours(8)
        };

        await httpContext.SignInAsync(
            IdentityConstants.ApplicationScheme,
            principal,
            authenticationProperties);
    }

    public async Task SignOutAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
    }
}