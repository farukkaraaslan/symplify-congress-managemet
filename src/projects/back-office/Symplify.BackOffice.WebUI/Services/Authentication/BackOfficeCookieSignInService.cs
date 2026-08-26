using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Symplify.BackOffice.Application.Services.Authentication;
using Symplify.BackOffice.WebUI.Services.Auth;
using SecurityClaim = System.Security.Claims.Claim;
using SecurityClaimTypes = System.Security.Claims.ClaimTypes;
using SecurityClaimsIdentity = System.Security.Claims.ClaimsIdentity;
using SecurityClaimsPrincipal = System.Security.Claims.ClaimsPrincipal;

namespace Symplify.BackOffice.WebUI.Services.Authentication;

public sealed class BackOfficeCookieSignInService : IBackOfficeCookieSignInService
{
    private readonly IConfiguration _configuration;

    public BackOfficeCookieSignInService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

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

        foreach (string operationClaim in user.OperationClaims.Where(claim => !string.IsNullOrWhiteSpace(claim)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new SecurityClaim(SecurityClaimTypes.Role, operationClaim));
        }

        if (user.OrganizationId.HasValue && user.OrganizationId.Value != Guid.Empty)
        {
            claims.Add(new SecurityClaim("OrganizationId", user.OrganizationId.Value.ToString("D")));

            if (!string.IsNullOrWhiteSpace(user.OrganizationSlug))
            {
                claims.Add(new SecurityClaim("OrganizationSlug", user.OrganizationSlug));
                AuthOrganizationContextCookie.Append(httpContext, user.OrganizationSlug);
            }

            if (!string.IsNullOrWhiteSpace(user.OrganizationName))
                claims.Add(new SecurityClaim("OrganizationName", user.OrganizationName));

            if (!string.IsNullOrWhiteSpace(user.OrganizationShortName))
                claims.Add(new SecurityClaim("OrganizationShortName", user.OrganizationShortName));
        }

        SecurityClaimsIdentity identity = new(
            claims,
            IdentityConstants.ApplicationScheme,
            SecurityClaimTypes.Name,
            SecurityClaimTypes.Role);

        SecurityClaimsPrincipal principal = new(identity);

        int expireMinutes = ResolveCookieExpireMinutes();
        bool slidingExpiration = ResolveSlidingExpiration();
        DateTimeOffset issuedUtc = DateTimeOffset.UtcNow;

        AuthenticationProperties authenticationProperties = new()
        {
            IsPersistent = rememberMe,
            AllowRefresh = slidingExpiration,
            IssuedUtc = issuedUtc,
            ExpiresUtc = issuedUtc.AddMinutes(expireMinutes)
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

    private int ResolveCookieExpireMinutes()
    {
        int expireMinutes = _configuration.GetValue<int?>("Authentication:Cookie:ExpireMinutes") ?? 30;
        return Math.Clamp(expireMinutes, 1, 1440);
    }

    private bool ResolveSlidingExpiration()
    {
        return _configuration.GetValue<bool?>("Authentication:Cookie:SlidingExpiration") ?? true;
    }
}
