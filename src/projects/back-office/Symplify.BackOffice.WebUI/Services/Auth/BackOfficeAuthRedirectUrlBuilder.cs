using System.Net;

namespace Symplify.BackOffice.WebUI.Services.Auth;

public static class BackOfficeAuthRedirectUrlBuilder
{
    private const string DefaultCulture = "tr-TR";
    private const string LoginControllerPath = "auth/login";
    private const string AccessDeniedControllerPath = "auth/access-denied";

    public static string BuildLoginRedirectUrl(HttpContext httpContext, string configuredLoginPath)
    {
        string culture = ResolveCulture(httpContext.Request.Path, configuredLoginPath);
        string returnUrl = BuildReturnUrl(httpContext);
        string loginPath = BuildLocalizedPath(httpContext, culture, LoginControllerPath);

        List<KeyValuePair<string, string?>> queryParameters = new()
        {
            new("ReturnUrl", returnUrl)
        };

        string? organization = ResolveOrganizationKey(httpContext, returnUrl);
        if (!string.IsNullOrWhiteSpace(organization))
            queryParameters.Insert(0, new KeyValuePair<string, string?>("org", organization));

        return loginPath + QueryString.Create(queryParameters).ToUriComponent();
    }

    public static string BuildAccessDeniedRedirectUrl(HttpContext httpContext, string configuredAccessDeniedPath)
    {
        string culture = ResolveCulture(httpContext.Request.Path, configuredAccessDeniedPath);
        string accessDeniedPath = BuildLocalizedPath(httpContext, culture, AccessDeniedControllerPath);

        string? organization = ResolveOrganizationKey(httpContext, BuildReturnUrl(httpContext));
        if (string.IsNullOrWhiteSpace(organization))
            return accessDeniedPath;

        return accessDeniedPath + QueryString.Create("org", organization).ToUriComponent();
    }

    private static string BuildReturnUrl(HttpContext httpContext)
    {
        string pathBase = httpContext.Request.PathBase.HasValue
            ? httpContext.Request.PathBase.Value ?? string.Empty
            : string.Empty;

        string path = httpContext.Request.Path.HasValue
            ? httpContext.Request.Path.Value ?? "/"
            : "/";

        string queryString = httpContext.Request.QueryString.HasValue
            ? httpContext.Request.QueryString.Value ?? string.Empty
            : string.Empty;

        return string.Concat(pathBase, path, queryString);
    }

    private static string BuildLocalizedPath(HttpContext httpContext, string culture, string controllerPath)
    {
        string pathBase = httpContext.Request.PathBase.HasValue
            ? httpContext.Request.PathBase.Value?.TrimEnd('/') ?? string.Empty
            : string.Empty;

        return $"{pathBase}/{culture}/{controllerPath}";
    }

    private static string ResolveCulture(PathString currentPath, string configuredPath)
    {
        string? cultureFromCurrentPath = GetFirstPathSegment(currentPath.Value);
        string? normalizedCulture = NormalizeCulture(cultureFromCurrentPath);
        if (!string.IsNullOrWhiteSpace(normalizedCulture))
            return normalizedCulture;

        string? cultureFromConfiguredPath = GetFirstPathSegment(configuredPath);
        normalizedCulture = NormalizeCulture(cultureFromConfiguredPath);
        return string.IsNullOrWhiteSpace(normalizedCulture)
            ? DefaultCulture
            : normalizedCulture;
    }

    private static string? ResolveOrganizationKey(HttpContext httpContext, string returnUrl)
    {
        return FirstNonEmpty(
            GetRequestQueryValue(httpContext, "org"),
            GetRequestQueryValue(httpContext, "organization"),
            GetRequestQueryValue(httpContext, "tenant"),
            GetQueryValueFromUrl(returnUrl, "org"),
            GetQueryValueFromUrl(returnUrl, "organization"),
            GetQueryValueFromUrl(returnUrl, "tenant"),
            httpContext.User.FindFirst("OrganizationSlug")?.Value,
            AuthOrganizationContextCookie.Read(httpContext));
    }

    private static string? GetRequestQueryValue(HttpContext httpContext, string key)
    {
        return httpContext.Request.Query.TryGetValue(key, out Microsoft.Extensions.Primitives.StringValues values)
            ? AuthOrganizationContextCookie.NormalizeOrganizationKey(values.FirstOrDefault())
            : null;
    }

    private static string? GetQueryValueFromUrl(string? url, string key)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        int queryStartIndex = url.IndexOf('?');
        if (queryStartIndex < 0 || queryStartIndex == url.Length - 1)
            return null;

        string query = url[(queryStartIndex + 1)..];
        foreach (string pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string[] parts = pair.Split('=', 2);
            string decodedKey = WebUtility.UrlDecode(parts[0]);
            if (!string.Equals(decodedKey, key, StringComparison.OrdinalIgnoreCase))
                continue;

            string? decodedValue = parts.Length > 1 ? WebUtility.UrlDecode(parts[1]) : null;
            return AuthOrganizationContextCookie.NormalizeOrganizationKey(decodedValue);
        }

        return null;
    }

    private static string? GetFirstPathSegment(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        return path
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
    }

    private static string? NormalizeCulture(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        string normalized = value.Trim();

        if (string.Equals(normalized, "tr", StringComparison.OrdinalIgnoreCase))
            return "tr-TR";

        if (string.Equals(normalized, "en", StringComparison.OrdinalIgnoreCase))
            return "en-US";

        if (string.Equals(normalized, "tr-TR", StringComparison.OrdinalIgnoreCase))
            return "tr-TR";

        if (string.Equals(normalized, "en-US", StringComparison.OrdinalIgnoreCase))
            return "en-US";

        return null;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (string? value in values)
        {
            string? normalized = AuthOrganizationContextCookie.NormalizeOrganizationKey(value);
            if (!string.IsNullOrWhiteSpace(normalized))
                return normalized;
        }

        return null;
    }
}
