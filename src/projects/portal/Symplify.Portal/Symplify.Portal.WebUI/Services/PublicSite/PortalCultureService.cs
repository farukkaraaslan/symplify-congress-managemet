using System.Globalization;
using Microsoft.Extensions.Options;
using Symplify.Portal.WebUI.Options;

namespace Symplify.Portal.WebUI.Services.PublicSite;

public sealed class PortalCultureService : IPortalCultureService
{
    private const string PortalCultureCookieName = "Symplify.Portal.Culture";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly PublicApiOptions _options;

    public PortalCultureService(IHttpContextAccessor httpContextAccessor, IOptions<PublicApiOptions> options)
    {
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
    }

    public string GetCurrentCulture()
    {
        HttpContext? httpContext = _httpContextAccessor.HttpContext;

        // Kullanıcının açık seçimi her zaman en güçlü kaynaktır.
        // Header'daki tarayıcı dili sadece ilk giriş fallback'i olmalıdır; dil seçicinin seçimini ezmemelidir.
        string? requested = ResolveExplicitSelection(httpContext);

        if (string.IsNullOrWhiteSpace(requested))
            requested = httpContext?.Request.Cookies[PortalCultureCookieName];

        // İlk ziyaret / cookie yok senaryosu: browser Accept-Language'a bakılır.
        if (string.IsNullOrWhiteSpace(requested))
            requested = ResolveFromAcceptLanguageHeader(httpContext);

        if (string.IsNullOrWhiteSpace(requested))
            requested = _options.DefaultCulture;

        return NormalizeCulture(requested, _options.DefaultCulture);
    }

    private static string? ResolveExplicitSelection(HttpContext? httpContext)
    {
        if (httpContext is null)
            return null;

        // Dil seçici ve doğrudan URL kullanımı için query en net seçimdir.
        string? queryCulture = httpContext.Request.Query["culture"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(queryCulture))
            return queryCulture;

        // Route culture kullanan URL'ler için destek: /tr-TR/...
        object? routeCulture = httpContext.Request.RouteValues["culture"];
        if (!string.IsNullOrWhiteSpace(routeCulture?.ToString()))
            return routeCulture.ToString();

        return null;
    }

    private static string? ResolveFromAcceptLanguageHeader(HttpContext? httpContext)
    {
        string? acceptLanguage = httpContext?.Request.Headers.AcceptLanguage.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(acceptLanguage))
            return null;

        string firstLanguage = acceptLanguage
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? string.Empty;

        return firstLanguage
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
    }

    private static string NormalizeCulture(string? culture, string? fallbackCulture)
    {
        string fallback = NormalizeKnownCulture(fallbackCulture) ?? "tr-TR";
        string? known = NormalizeKnownCulture(culture);

        if (!string.IsNullOrWhiteSpace(known))
            return known;

        if (string.IsNullOrWhiteSpace(culture))
            return fallback;

        try
        {
            CultureInfo cultureInfo = CultureInfo.GetCultureInfo(culture.Trim());
            return cultureInfo.Name;
        }
        catch (CultureNotFoundException)
        {
            return fallback;
        }
    }

    private static string? NormalizeKnownCulture(string? culture)
    {
        return culture?.Trim().ToLowerInvariant() switch
        {
            "tr" or "tr-tr" => "tr-TR",
            "en" or "en-us" => "en-US",
            "en-gb" => "en-GB",
            "ru" or "ru-ru" => "ru-RU",
            "ar" or "ar-sa" => "ar-SA",
            "de" or "de-de" => "de-DE",
            "fr" or "fr-fr" => "fr-FR",
            "es" or "es-es" => "es-ES",
            _ => null
        };
    }
}
