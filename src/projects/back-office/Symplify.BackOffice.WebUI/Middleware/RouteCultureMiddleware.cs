using System.Globalization;
using Symplify.BackOffice.Application.Services.Localization;

namespace Symplify.BackOffice.WebUI.Middleware;

public sealed class RouteCultureMiddleware
{
    private const string CultureCookieName = "Symplify.BackOffice.Culture";

    private readonly RequestDelegate _next;
    private readonly ILogger<RouteCultureMiddleware> _logger;

    public RouteCultureMiddleware(RequestDelegate next, ILogger<RouteCultureMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IApplicationLanguageProvider applicationLanguageProvider)
    {
        string? requestedCulture = ResolveRequestedCulture(context);

        ApplicationLanguageDto resolvedLanguage = await ResolveLanguageAsync(
            requestedCulture,
            applicationLanguageProvider,
            context.RequestAborted);

        ApplyCulture(resolvedLanguage.Culture);

        context.Items["CurrentCulture"] = resolvedLanguage.Culture;
        context.Items["CurrentLanguageId"] = resolvedLanguage.Id;
        context.Items["CurrentLanguageName"] = resolvedLanguage.Name;
        context.Items["IsDefaultLanguage"] = resolvedLanguage.IsDefault;

        PersistCultureCookieIfNeeded(context, requestedCulture, resolvedLanguage.Culture);

        await _next(context);
    }

    private async Task<ApplicationLanguageDto> ResolveLanguageAsync(
        string? requestedCulture,
        IApplicationLanguageProvider applicationLanguageProvider,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(requestedCulture))
        {
            ApplicationLanguageDto? requestedLanguage = await applicationLanguageProvider.GetByCultureAsync(
                requestedCulture,
                cancellationToken);

            if (requestedLanguage is not null)
                return requestedLanguage;

            _logger.LogDebug(
                "Requested culture '{RequestedCulture}' was not found in database. Falling back to default language.",
                requestedCulture);
        }

        return await applicationLanguageProvider.GetDefaultLanguageAsync(cancellationToken);
    }

    private static string? ResolveRequestedCulture(HttpContext context)
    {
        return GetRouteCulture(context) ??
               GetQueryCulture(context) ??
               GetCookieCulture(context) ??
               GetAcceptLanguageCulture(context);
    }

    private static string? GetRouteCulture(HttpContext context)
    {
        if (context.Request.RouteValues.TryGetValue("culture", out object? routeCultureValue))
        {
            string? normalizedRouteCulture = NormalizeCultureCandidate(routeCultureValue?.ToString());

            if (!string.IsNullOrWhiteSpace(normalizedRouteCulture))
                return normalizedRouteCulture;
        }

        string? firstSegment = context.Request.Path.Value?
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(firstSegment) || !LooksLikeCultureSegment(firstSegment))
            return null;

        return NormalizeCultureCandidate(firstSegment);
    }

    private static string? GetQueryCulture(HttpContext context)
    {
        string? culture = context.Request.Query["culture"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(culture))
            culture = context.Request.Query["ui-culture"].FirstOrDefault();

        return NormalizeCultureCandidate(culture);
    }

    private static string? GetCookieCulture(HttpContext context)
    {
        bool exists = context.Request.Cookies.TryGetValue(CultureCookieName, out string? culture);
        return exists ? NormalizeCultureCandidate(culture) : null;
    }

    private static string? GetAcceptLanguageCulture(HttpContext context)
    {
        string? acceptLanguage = context.Request.Headers.AcceptLanguage.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(acceptLanguage))
            return null;

        IEnumerable<string> candidates = acceptLanguage
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Where(x => x != "*");

        foreach (string candidate in candidates)
        {
            string? normalizedCulture = NormalizeCultureCandidate(candidate);

            if (!string.IsNullOrWhiteSpace(normalizedCulture))
                return normalizedCulture;
        }

        return null;
    }

    private static void ApplyCulture(string culture)
    {
        CultureInfo cultureInfo = CultureInfo.GetCultureInfo(culture);
        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;
    }

    private static void PersistCultureCookieIfNeeded(HttpContext context, string? requestedCulture, string resolvedCulture)
    {
        if (string.IsNullOrWhiteSpace(requestedCulture))
            return;

        if (!string.Equals(requestedCulture, resolvedCulture, StringComparison.OrdinalIgnoreCase))
            return;

        context.Response.Cookies.Append(
            CultureCookieName,
            resolvedCulture,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });
    }

    private static string? NormalizeCultureCandidate(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
            return null;

        culture = culture.Trim().Replace("_", "-");

        try
        {
            CultureInfo cultureInfo = culture.Length == 2
                ? CultureInfo.CreateSpecificCulture(culture)
                : CultureInfo.GetCultureInfo(culture);

            return cultureInfo.Name;
        }
        catch (CultureNotFoundException)
        {
            return culture;
        }
    }

    private static bool LooksLikeCultureSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        value = value.Trim();

        if (value.Length is < 2 or > 15)
            return false;

        return value.Any(x => x == '-' || x == '_') || value.Length == 2;
    }
}
