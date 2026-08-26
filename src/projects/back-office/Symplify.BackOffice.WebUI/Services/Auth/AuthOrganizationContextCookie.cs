namespace Symplify.BackOffice.WebUI.Services.Auth;

public static class AuthOrganizationContextCookie
{
    public const string CookieName = ".Symplify.BackOffice.Organization";

    private static readonly TimeSpan CookieLifetime = TimeSpan.FromDays(180);

    public static void Append(HttpContext httpContext, string? organizationSlug)
    {
        string? normalizedSlug = NormalizeOrganizationKey(organizationSlug);
        if (string.IsNullOrWhiteSpace(normalizedSlug))
            return;

        httpContext.Response.Cookies.Append(
            CookieName,
            normalizedSlug,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Expires = DateTimeOffset.UtcNow.Add(CookieLifetime)
            });
    }

    public static string? Read(HttpContext httpContext)
    {
        return httpContext.Request.Cookies.TryGetValue(CookieName, out string? value)
            ? NormalizeOrganizationKey(value)
            : null;
    }

    public static string? NormalizeOrganizationKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        string normalized = value.Trim().Trim('/');
        if (normalized.Length == 0 || normalized.Length > 100)
            return null;

        foreach (char character in normalized)
        {
            if (char.IsLetterOrDigit(character) || character is '-' or '_' or '.')
                continue;

            return null;
        }

        return normalized;
    }
}
