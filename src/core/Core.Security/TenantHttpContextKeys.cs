namespace Core.Utilities;

/// <summary>
/// Keys for tenant workspace override values.
/// Persisted in both <c>HttpContext.Items</c> (current request) and Session (cross-request).
/// </summary>
public static class TenantHttpContextKeys
{
    public const string OverrideTenantId = "OverrideTenantId";
    public const string OverrideCongressId = "OverrideCongressId";
}
