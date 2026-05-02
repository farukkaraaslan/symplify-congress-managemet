using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Symplify.BackOffice.Domain.Identity;

namespace Symplify.BackOffice.WebUI.Services.Authentication;

public sealed class BackOfficeClaimsTransformation : IClaimsTransformation
{
    private const string PermissionClaimType = "Permission";
    private const string ClaimsLoadedClaimType = "BackOffice.ClaimsLoaded";
    private const string SuperAdminRoleName = "SuperAdmin";

    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly IMemoryCache _memoryCache;

    public BackOfficeClaimsTransformation(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        IMemoryCache memoryCache)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _memoryCache = memoryCache;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return principal;

        if (principal.HasClaim(claim => claim.Type == ClaimsLoadedClaimType))
            return principal;

        string? userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return principal;

        if (!Guid.TryParse(userId, out Guid parsedUserId))
            return principal;

        ClaimsIdentity identity = principal.Identity as ClaimsIdentity
            ?? new ClaimsIdentity();

        IReadOnlyCollection<string> operationClaims = await GetOperationClaimsAsync(parsedUserId);

        foreach (string operationClaim in operationClaims)
        {
            if (string.IsNullOrWhiteSpace(operationClaim))
                continue;

            if (!principal.HasClaim(ClaimTypes.Role, operationClaim))
                identity.AddClaim(new Claim(ClaimTypes.Role, operationClaim));

            if (!principal.HasClaim(PermissionClaimType, operationClaim))
                identity.AddClaim(new Claim(PermissionClaimType, operationClaim));
        }

        identity.AddClaim(new Claim(ClaimsLoadedClaimType, "true"));

        return principal;
    }

    private async Task<IReadOnlyCollection<string>> GetOperationClaimsAsync(Guid userId)
    {
        string cacheKey = $"BackOffice:UserOperationClaims:{userId}";

        if (_memoryCache.TryGetValue(cacheKey, out IReadOnlyCollection<string>? cachedClaims) &&
            cachedClaims is not null)
        {
            return cachedClaims;
        }

        AppUser? user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
            return Array.Empty<string>();

        IList<string> roles = await _userManager.GetRolesAsync(user);

        List<string> operationClaims = new();

        foreach (string roleName in roles)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                continue;

            // Role adı da claim olarak eklenir.
            operationClaims.Add(roleName);

            AppRole? role = await _roleManager.FindByNameAsync(roleName);

            if (role is null)
                continue;

            IList<Claim> roleClaims = await _roleManager.GetClaimsAsync(role);

            operationClaims.AddRange(
                roleClaims
                    .Where(claim => claim.Type == PermissionClaimType)
                    .Select(claim => claim.Value));
        }

        operationClaims = operationClaims
            .Where(claim => !string.IsNullOrWhiteSpace(claim))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(claim => claim)
            .ToList();

        _memoryCache.Set(
            cacheKey,
            operationClaims,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

        return operationClaims;
    }
}