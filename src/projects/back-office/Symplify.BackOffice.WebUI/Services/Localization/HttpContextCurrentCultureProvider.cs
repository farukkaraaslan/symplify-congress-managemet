using Symplify.BackOffice.Application.Services.Localization;

namespace Symplify.BackOffice.WebUI.Services.Localization;

public sealed class HttpContextCurrentCultureProvider : ICurrentCultureProvider
{
    private const string CultureCookieName = "Symplify.BackOffice.Culture";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentCultureProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetCurrentCulture()
    {
        HttpContext? httpContext = _httpContextAccessor.HttpContext;

        return GetRouteCulture(httpContext) ??
               GetQueryCulture(httpContext) ??
               GetCookieCulture(httpContext) ??
               GetAcceptLanguageCulture(httpContext);
    }

    private static string? GetRouteCulture(HttpContext? httpContext)
    {
        string? culture = httpContext?.Request.RouteValues["culture"]?.ToString();
        return NormalizeCultureCandidate(culture);
    }

    private static string? GetQueryCulture(HttpContext? httpContext)
    {
        if (httpContext is null)
            return null;

        string? culture = httpContext.Request.Query["culture"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(culture))
            culture = httpContext.Request.Query["ui-culture"].FirstOrDefault();

        return NormalizeCultureCandidate(culture);
    }

    private static string? GetCookieCulture(HttpContext? httpContext)
    {
        if (httpContext is null)
            return null;

        bool exists = httpContext.Request.Cookies.TryGetValue(CultureCookieName, out string? culture);
        return exists ? NormalizeCultureCandidate(culture) : null;
    }

    private static string? GetAcceptLanguageCulture(HttpContext? httpContext)
    {
        string? acceptLanguage = httpContext?.Request.Headers.AcceptLanguage.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(acceptLanguage))
            return null;

        string? culture = acceptLanguage
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault()
            ?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        return NormalizeCultureCandidate(culture);
    }

    private static string? NormalizeCultureCandidate(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
            return null;

        culture = culture.Trim().Replace("_", "-");

        return culture.ToLowerInvariant() switch
        {
            "tr" => "tr-TR",
            "tr-tr" => "tr-TR",
            "en" => "en-US",
            "en-us" => "en-US",
            _ => culture
        };
    }
}
