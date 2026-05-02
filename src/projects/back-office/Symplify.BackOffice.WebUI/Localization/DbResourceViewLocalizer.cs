using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.WebUI.Localization;

public sealed class DbResourceViewLocalizer : IBackOfficeViewLocalizer
{
    private const string SafeFallbackCulture = "tr-TR";

    private readonly BackOfficeDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DbResourceViewLocalizer(
        BackOfficeDbContext context,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public LocalizedHtmlString this[string name]
    {
        get
        {
            string value = GetStringValue(name);

            return ToLocalizedHtmlString(name, value);
        }
    }

    public LocalizedHtmlString this[string name, params object[] arguments]
    {
        get
        {
            string value = GetStringValue(name);

            string formattedValue = arguments.Length == 0
                ? value
                : string.Format(CultureInfo.CurrentCulture, value, arguments);

            return ToLocalizedHtmlString(name, formattedValue);
        }
    }

    public LocalizedString GetString(string name)
    {
        string value = GetStringValue(name);

        return ToLocalizedString(name, value);
    }

    public LocalizedString GetString(string name, params object[] arguments)
    {
        string value = GetStringValue(name);

        string formattedValue = arguments.Length == 0
            ? value
            : string.Format(CultureInfo.CurrentCulture, value, arguments);

        return ToLocalizedString(name, formattedValue);
    }

    public string GetStringValue(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        string culture = ResolveCulture();

        string? value = GetValueByCulture(key, culture);

        if (!string.IsNullOrWhiteSpace(value))
            return value;

        string defaultCulture = GetDefaultCulture();

        if (!string.Equals(defaultCulture, culture, StringComparison.OrdinalIgnoreCase))
        {
            value = GetValueByCulture(key, defaultCulture);

            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return key;
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        string culture = ResolveCulture();

        List<LocalizedString> values = GetAllStringsByCulture(culture);

        if (values.Count > 0)
            return values;

        string defaultCulture = GetDefaultCulture();

        if (!string.Equals(defaultCulture, culture, StringComparison.OrdinalIgnoreCase))
            return GetAllStringsByCulture(defaultCulture);

        return values;
    }

    private string? GetValueByCulture(string key, string culture)
    {
        string normalizedCulture = NormalizeCultureFromDatabase(culture);

        return (
            from resourceValue in _context.ResourceValues.AsNoTracking().IgnoreQueryFilters()
            join resourceKey in _context.ResourceKeys.AsNoTracking().IgnoreQueryFilters()
                on resourceValue.ResourceKeyId equals resourceKey.Id
            join language in _context.Languages.AsNoTracking().IgnoreQueryFilters()
                on resourceValue.LanguageId equals language.Id
            where resourceKey.KeyName == key &&
                  language.Culture == normalizedCulture &&
                  language.IsActive
            select resourceValue.Value
        ).FirstOrDefault();
    }

    private List<LocalizedString> GetAllStringsByCulture(string culture)
    {
        string normalizedCulture = NormalizeCultureFromDatabase(culture);

        return (
            from resourceValue in _context.ResourceValues.AsNoTracking().IgnoreQueryFilters()
            join resourceKey in _context.ResourceKeys.AsNoTracking().IgnoreQueryFilters()
                on resourceValue.ResourceKeyId equals resourceKey.Id
            join language in _context.Languages.AsNoTracking().IgnoreQueryFilters()
                on resourceValue.LanguageId equals language.Id
            where language.Culture == normalizedCulture &&
                  language.IsActive
            select new LocalizedString(
                resourceKey.KeyName,
                resourceValue.Value,
                resourceNotFound: false)
        ).ToList();
    }

    private string ResolveCulture()
    {
        HttpContext? httpContext = _httpContextAccessor.HttpContext;

        string? formCulture = null;

        if (httpContext?.Request.HasFormContentType == true)
            formCulture = httpContext.Request.Form["culture"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(formCulture))
            return NormalizeCultureFromDatabase(formCulture);

        string? queryCulture = httpContext?.Request.Query["culture"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(queryCulture))
            return NormalizeCultureFromDatabase(queryCulture);

        string? headerCulture = httpContext?.Request.Headers["X-Culture"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(headerCulture))
            return NormalizeCultureFromDatabase(headerCulture);

        string? routeCulture = httpContext?.Request.RouteValues["culture"]?.ToString();

        if (!string.IsNullOrWhiteSpace(routeCulture))
            return NormalizeCultureFromDatabase(routeCulture);

        string? pathCulture = httpContext?.Request.Path.Value?
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(pathCulture))
            return NormalizeCultureFromDatabase(pathCulture);

        IRequestCultureFeature? requestCultureFeature = httpContext?.Features.Get<IRequestCultureFeature>();
        string? requestCulture = requestCultureFeature?.RequestCulture.Culture.Name;

        if (!string.IsNullOrWhiteSpace(requestCulture))
            return NormalizeCultureFromDatabase(requestCulture);

        return NormalizeCultureFromDatabase(CultureInfo.CurrentUICulture.Name);
    }

    private string NormalizeCultureFromDatabase(string? culture)
    {
        string? requestedCulture = culture?.Trim();

        if (!string.IsNullOrWhiteSpace(requestedCulture))
        {
            string requestedCultureLower = requestedCulture.ToLowerInvariant();

            string? matchedCulture = _context.Languages
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(language => language.IsActive)
                .Where(language =>
                    language.Culture.ToLower() == requestedCultureLower ||
                    language.TwoLetterIsoCode.ToLower() == requestedCultureLower)
                .OrderBy(language => language.Order)
                .Select(language => language.Culture)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(matchedCulture))
                return matchedCulture;
        }

        return GetDefaultCulture();
    }

    private string GetDefaultCulture()
    {
        string? defaultCulture = _context.Languages
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(language => language.IsActive && language.IsDefault)
            .OrderBy(language => language.Order)
            .Select(language => language.Culture)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(defaultCulture))
            return defaultCulture;

        string? firstActiveCulture = _context.Languages
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(language => language.IsActive)
            .OrderBy(language => language.Order)
            .Select(language => language.Culture)
            .FirstOrDefault();

        return firstActiveCulture ?? SafeFallbackCulture;
    }

    private static LocalizedHtmlString ToLocalizedHtmlString(string name, string value)
    {
        return new LocalizedHtmlString(
            name,
            value,
            isResourceNotFound: value == name);
    }

    private static LocalizedString ToLocalizedString(string name, string value)
    {
        return new LocalizedString(
            name,
            value,
            resourceNotFound: value == name);
    }
}