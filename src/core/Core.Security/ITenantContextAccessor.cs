namespace Core.Utilities;

/// <summary>
/// Resolved tenant/congress scope for the current HTTP request from claims and optional SuperAdmin override.
/// </summary>
public interface ITenantContextAccessor
{
    /// <summary>From claims unless SuperAdmin has set an override.</summary>
    Guid? EffectiveTenantId { get; }

    /// <summary>Active congress from claims or override (never from URL).</summary>
    Guid? EffectiveActiveCongressId { get; }

    /// <summary>SuperAdmin / elevated: temporarily scope queries to a tenant and optional congress.</summary>
    void SetOverrideContext(Guid tenantId, Guid? congressId);

    void ClearOverrideContext();

    bool HasOverride { get; }
}
