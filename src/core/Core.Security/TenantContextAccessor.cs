using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Core.Utilities;

public sealed class TenantContextAccessor : ITenantContextAccessor
{
    public const string TenantIdClaimType = "TenantId";
    public const string ActiveCongressIdClaimType = "ActiveCongressId";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private HttpContext? Http => _httpContextAccessor.HttpContext;

    public bool HasOverride
    {
        get
        {
            var ctx = Http;
            if (ctx is null)
                return false;
            if (!IsSuperAdmin(ctx))
                return false;

            if (TryReadOverrideFromItems(ctx, out _))
                return true;

            return TryReadOverrideFromSession(ctx, out _);
        }
    }

    public Guid? EffectiveTenantId
    {
        get
        {
            var ctx = Http;
            if (ctx is not null && IsSuperAdmin(ctx))
            {
                if (TryReadOverrideFromItems(ctx, out var tenantFromItems))
                    return tenantFromItems;

                if (TryReadOverrideFromSession(ctx, out var tenantFromSession))
                {
                    ctx.Items[TenantHttpContextKeys.OverrideTenantId] = tenantFromSession;
                    return tenantFromSession;
                }
            }

            return ReadGuidClaim(TenantIdClaimType);
        }
    }

    public Guid? EffectiveActiveCongressId
    {
        get
        {
            var ctx = Http;
            if (ctx is not null && IsSuperAdmin(ctx))
            {
                if (TryReadOverrideFromItems(ctx, out _) || TryReadOverrideFromSession(ctx, out _))
                {
                    if (ctx.Items.TryGetValue(TenantHttpContextKeys.OverrideCongressId, out var oc) &&
                        oc is Guid overrideCongress &&
                        overrideCongress != Guid.Empty)
                        return overrideCongress;

                    var sessionCongressRaw = ReadSessionString(GetSession(ctx), TenantHttpContextKeys.OverrideCongressId);
                    if (Guid.TryParse(sessionCongressRaw, out var sessionCongress) && sessionCongress != Guid.Empty)
                    {
                        ctx.Items[TenantHttpContextKeys.OverrideCongressId] = sessionCongress;
                        return sessionCongress;
                    }

                    return null;
                }

                if (ctx.Items.TryGetValue(TenantHttpContextKeys.OverrideCongressId, out var c) &&
                    c is Guid cg &&
                    cg != Guid.Empty)
                    return cg;

                if (TryReadOverrideFromSession(ctx, out _) &&
                    Guid.TryParse(ReadSessionString(GetSession(ctx), TenantHttpContextKeys.OverrideCongressId), out var scg) &&
                    scg != Guid.Empty)
                {
                    ctx.Items[TenantHttpContextKeys.OverrideCongressId] = scg;
                    return scg;
                }
            }

            return ReadGuidClaim(ActiveCongressIdClaimType);
        }
    }

    public void SetOverrideContext(Guid tenantId, Guid? congressId)
    {
        var ctx = Http ?? throw new InvalidOperationException("No HttpContext.");
        var congress = congressId ?? Guid.Empty;
        ctx.Items[TenantHttpContextKeys.OverrideTenantId] = tenantId;
        ctx.Items[TenantHttpContextKeys.OverrideCongressId] = congress;
        var session = GetSession(ctx);
        WriteSessionString(session, TenantHttpContextKeys.OverrideTenantId, tenantId.ToString());
        WriteSessionString(session, TenantHttpContextKeys.OverrideCongressId, congress.ToString());
    }

    public void ClearOverrideContext()
    {
        var ctx = Http;
        if (ctx is null)
            return;
        ctx.Items.Remove(TenantHttpContextKeys.OverrideTenantId);
        ctx.Items.Remove(TenantHttpContextKeys.OverrideCongressId);
        var session = GetSession(ctx);
        session?.Remove(TenantHttpContextKeys.OverrideTenantId);
        session?.Remove(TenantHttpContextKeys.OverrideCongressId);
    }

    private Guid? ReadGuidClaim(string type)
    {
        var v = Http?.User?.FindFirst(type)?.Value;
        if (string.IsNullOrWhiteSpace(v) || !Guid.TryParse(v, out var g) || g == Guid.Empty)
            return null;
        return g;
    }

    private static bool TryReadOverrideFromItems(HttpContext ctx, out Guid tenantId)
    {
        if (ctx.Items.TryGetValue(TenantHttpContextKeys.OverrideTenantId, out var t) &&
            t is Guid g &&
            g != Guid.Empty)
        {
            tenantId = g;
            return true;
        }

        tenantId = Guid.Empty;
        return false;
    }

    private static bool TryReadOverrideFromSession(HttpContext ctx, out Guid tenantId)
    {
        var session = GetSession(ctx);
        if (session is not null &&
            Guid.TryParse(ReadSessionString(session, TenantHttpContextKeys.OverrideTenantId), out var g) &&
            g != Guid.Empty)
        {
            tenantId = g;
            return true;
        }

        tenantId = Guid.Empty;
        return false;
    }

    private static ISession? GetSession(HttpContext ctx)
        => ctx.Features.Get<ISessionFeature>()?.Session;

    private static string? ReadSessionString(ISession? session, string key)
    {
        if (session is null || !session.TryGetValue(key, out var bytes) || bytes is null || bytes.Length == 0)
            return null;
        return Encoding.UTF8.GetString(bytes);
    }

    private static void WriteSessionString(ISession? session, string key, string value)
    {
        if (session is null)
            return;
        session.Set(key, Encoding.UTF8.GetBytes(value));
    }

    private static bool IsSuperAdmin(HttpContext ctx)
        => ctx.User?.Identity?.IsAuthenticated == true && ctx.User.IsInRole("SuperAdmin");
}
