using System.Globalization;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Symplify.BackOffice.Domain.Localization;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.WebUI.Localization;

public sealed class BackOfficeResourceProvider : IBackOfficeResourceProvider
{
    private const string SafeFallbackCulture = "tr-TR";

    private static readonly TimeSpan ResourceCacheDuration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan LanguageCacheDuration = TimeSpan.FromMinutes(30);

    private readonly BackOfficeDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMemoryCache _cache;

    public BackOfficeResourceProvider(
        BackOfficeDbContext context,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache cache)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _cache = cache;
    }

    public string ResolveCurrentCulture()
    {
        HttpContext? httpContext = _httpContextAccessor.HttpContext;

        string? formCulture = null;

        if (httpContext?.Request.HasFormContentType == true)
            formCulture = httpContext.Request.Form["culture"].FirstOrDefault();

        string? queryCulture = httpContext?.Request.Query["culture"].FirstOrDefault();
        string? headerCulture = httpContext?.Request.Headers["X-Culture"].FirstOrDefault();
        string? routeCulture = httpContext?.Request.RouteValues["culture"]?.ToString();
        string? pathCulture = httpContext?.Request.Path.Value?
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        IRequestCultureFeature? requestCultureFeature = httpContext?.Features.Get<IRequestCultureFeature>();
        string? requestCulture = requestCultureFeature?.RequestCulture.Culture.Name;

        return NormalizeCulture(
            formCulture
            ?? queryCulture
            ?? headerCulture
            ?? routeCulture
            ?? pathCulture
            ?? requestCulture
            ?? CultureInfo.CurrentUICulture.Name);
    }

    public string NormalizeCulture(string? culture)
    {
        string requestedCulture = NormalizeCultureCandidate(culture);
        string cacheKey = $"backoffice-localization:culture:{requestedCulture.ToLowerInvariant()}";

        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = LanguageCacheDuration;

            string requestedCultureLower = requestedCulture.ToLowerInvariant();

            string? matchedCulture = _context.Languages
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(language => language.DeletedDate == null && language.IsActive)
                .Where(language =>
                    language.Culture.ToLower() == requestedCultureLower ||
                    (language.TwoLetterIsoCode != null && language.TwoLetterIsoCode.ToLower() == requestedCultureLower))
                .OrderBy(language => language.Order)
                .Select(language => language.Culture)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(matchedCulture))
                return matchedCulture;

            return GetDefaultCulture();
        }) ?? SafeFallbackCulture;
    }

    public string GetStringValue(string key, string? culture = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        string normalizedCulture = NormalizeCulture(culture ?? ResolveCurrentCulture());
        string cacheKey = $"backoffice-localization:string:{normalizedCulture}:{key}";

        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ResourceCacheDuration;

            LanguageLookup languageLookup = GetLanguageLookup(normalizedCulture);

            string? value = (
                from resourceValue in _context.ResourceValues.AsNoTracking().IgnoreQueryFilters()
                join resourceKey in _context.ResourceKeys.AsNoTracking().IgnoreQueryFilters()
                    on resourceValue.ResourceKeyId equals resourceKey.Id
                where resourceKey.DeletedDate == null &&
                      resourceValue.DeletedDate == null &&
                      resourceKey.KeyName == key &&
                      (resourceValue.LanguageId == languageLookup.RequestedLanguageId ||
                       resourceValue.LanguageId == languageLookup.DefaultLanguageId)
                orderby resourceValue.LanguageId == languageLookup.RequestedLanguageId descending
                select resourceValue.Value
            ).FirstOrDefault();

            return string.IsNullOrWhiteSpace(value) ? key : value;
        }) ?? key;
    }

    public IReadOnlyDictionary<string, string> GetResourcesByPrefixes(
        IEnumerable<string> prefixes,
        string? culture = null)
    {
        List<string> normalizedPrefixes = NormalizePrefixes(prefixes);

        if (normalizedPrefixes.Count == 0)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        string normalizedCulture = NormalizeCulture(culture ?? ResolveCurrentCulture());
        string prefixHash = CreatePrefixHash(normalizedPrefixes);
        string cacheKey = $"backoffice-localization:client-resources:{normalizedCulture}:{prefixHash}";

        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ResourceCacheDuration;

            LanguageLookup languageLookup = GetLanguageLookup(normalizedCulture);
            Expression<Func<ResourceKey, bool>> prefixPredicate = BuildPrefixPredicate(normalizedPrefixes);

            List<ResourceRow> rows = (
                from resourceKey in _context.ResourceKeys
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(key => key.DeletedDate == null)
                    .Where(prefixPredicate)
                join resourceValue in _context.ResourceValues.AsNoTracking().IgnoreQueryFilters()
                    on resourceKey.Id equals resourceValue.ResourceKeyId
                where resourceValue.DeletedDate == null &&
                      (resourceValue.LanguageId == languageLookup.RequestedLanguageId ||
                       resourceValue.LanguageId == languageLookup.DefaultLanguageId)
                select new ResourceRow(
                    resourceKey.KeyName,
                    resourceValue.Value,
                    resourceValue.LanguageId)
            ).ToList();

            return rows
                .Where(row => !string.IsNullOrWhiteSpace(row.KeyName))
                .GroupBy(row => row.KeyName, StringComparer.OrdinalIgnoreCase)
                .Select(group => group
                    .OrderByDescending(row => row.LanguageId == languageLookup.RequestedLanguageId)
                    .First())
                .Where(row => !string.IsNullOrWhiteSpace(row.Value))
                .ToDictionary(
                    row => row.KeyName,
                    row => row.Value,
                    StringComparer.OrdinalIgnoreCase);
        }) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    private LanguageLookup GetLanguageLookup(string culture)
    {
        string normalizedCulture = NormalizeCulture(culture);
        string cacheKey = $"backoffice-localization:language-lookup:{normalizedCulture}";

        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = LanguageCacheDuration;

            Language? defaultLanguage = _context.Languages
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(language => language.DeletedDate == null && language.IsActive && language.IsDefault)
                .OrderBy(language => language.Order)
                .FirstOrDefault();

            defaultLanguage ??= _context.Languages
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(language => language.DeletedDate == null && language.IsActive)
                .OrderBy(language => language.Order)
                .FirstOrDefault();

            if (defaultLanguage is null)
                throw new InvalidOperationException("No active language is configured for BackOffice localization.");

            Language? requestedLanguage = _context.Languages
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(language => language.DeletedDate == null && language.IsActive)
                .FirstOrDefault(language => language.Culture == normalizedCulture);

            requestedLanguage ??= defaultLanguage;

            return new LanguageLookup(
                requestedLanguage.Id,
                defaultLanguage.Id,
                requestedLanguage.Culture,
                defaultLanguage.Culture);
        })!;
    }

    private string GetDefaultCulture()
    {
        string cacheKey = "backoffice-localization:default-culture";

        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = LanguageCacheDuration;

            string? defaultCulture = _context.Languages
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(language => language.DeletedDate == null && language.IsActive && language.IsDefault)
                .OrderBy(language => language.Order)
                .Select(language => language.Culture)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(defaultCulture))
                return defaultCulture;

            string? firstActiveCulture = _context.Languages
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(language => language.DeletedDate == null && language.IsActive)
                .OrderBy(language => language.Order)
                .Select(language => language.Culture)
                .FirstOrDefault();

            return firstActiveCulture ?? SafeFallbackCulture;
        }) ?? SafeFallbackCulture;
    }

    private static string NormalizeCultureCandidate(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
            return SafeFallbackCulture;

        string normalizedCulture = culture.Trim().Replace("_", "-");

        return normalizedCulture.ToLowerInvariant() switch
        {
            "tr" => "tr-TR",
            "tr-tr" => "tr-TR",
            "en" => "en-US",
            "en-us" => "en-US",
            _ => normalizedCulture
        };
    }

    List<string> NormalizePrefixes(IEnumerable<string> prefixes)
    {
        return prefixes
            .Where(prefix => !string.IsNullOrWhiteSpace(prefix))
            .Select(prefix => prefix.Trim())
            .Select(prefix => prefix.EndsWith(".", StringComparison.Ordinal) ? prefix : $"{prefix}.")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(prefix => prefix, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static Expression<Func<ResourceKey, bool>> BuildPrefixPredicate(IReadOnlyCollection<string> prefixes)
    {
        ParameterExpression parameter = Expression.Parameter(typeof(ResourceKey), "resourceKey");
        MemberExpression keyNameProperty = Expression.Property(parameter, nameof(ResourceKey.KeyName));
        System.Reflection.MethodInfo startsWithMethod = typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) })
            ?? throw new InvalidOperationException("string.StartsWith(string) method could not be found.");

        Expression? body = null;

        foreach (string prefix in prefixes)
        {
            MethodCallExpression startsWithCall = Expression.Call(
                keyNameProperty,
                startsWithMethod,
                Expression.Constant(prefix));

            body = body is null
                ? startsWithCall
                : Expression.OrElse(body, startsWithCall);
        }

        body ??= Expression.Constant(false);

        return Expression.Lambda<Func<ResourceKey, bool>>(body, parameter);
    }

    private static string CreatePrefixHash(IReadOnlyCollection<string> prefixes)
    {
        string value = string.Join('|', prefixes.OrderBy(prefix => prefix, StringComparer.OrdinalIgnoreCase));
        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));

        return Convert.ToHexString(hashBytes)[..16].ToLowerInvariant();
    }

    private sealed record LanguageLookup(
        Guid RequestedLanguageId,
        Guid DefaultLanguageId,
        string RequestedCulture,
        string DefaultCulture);

    private sealed record ResourceRow(
        string KeyName,
        string Value,
        Guid LanguageId);
}
