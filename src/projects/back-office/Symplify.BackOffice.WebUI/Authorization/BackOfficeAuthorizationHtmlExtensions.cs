using System.Security.Claims;

namespace Symplify.BackOffice.WebUI.Authorization;

public static class BackOfficeAuthorizationHtmlExtensions
{
    private const string PermissionClaimType = "Permission";
    private const string SuperAdminRoleName = "SuperAdmin";

    public static bool HasBackOfficePermission(this ClaimsPrincipal user, params string[] permissions)
    {
        if (user.Identity?.IsAuthenticated != true)
            return false;

        if (user.IsInRole(SuperAdminRoleName) || HasClaimValue(user, SuperAdminRoleName))
            return true;

        return permissions.Any(permission => HasClaimValue(user, permission));
    }

    public static bool CanReadBackOfficeSection(this ClaimsPrincipal user, string section)
    {
        return user.HasBackOfficePermission($"{section}.Admin", $"{section}.Read");
    }

    private static bool HasClaimValue(ClaimsPrincipal user, string value)
    {
        return user.Claims.Any(claim =>
            (string.Equals(claim.Type, PermissionClaimType, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(claim.Type, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase)) &&
            string.Equals(claim.Value, value, StringComparison.OrdinalIgnoreCase));
    }
}
