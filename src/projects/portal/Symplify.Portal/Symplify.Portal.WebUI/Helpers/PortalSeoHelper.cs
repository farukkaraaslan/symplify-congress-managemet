using System.Globalization;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.WebUtilities;
using Symplify.Portal.WebUI.Models.PublicSite;
using Symplify.Portal.WebUI.Models.Seo;

namespace Symplify.Portal.WebUI.Helpers;

public static partial class PortalSeoHelper
{
    private const int RecommendedDescriptionLength = 165;

    private static readonly JsonSerializerOptions JsonLdSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    public static PortalSeoMetadata BuildMetadata(
        HttpContext httpContext,
        ViewDataDictionary viewData,
        PublicSiteBootstrapResponse shell,
        string currentCulture,
        IReadOnlyCollection<PublicLanguageResponse> availableLanguages,
        string logoLightUrl,
        string logoDarkUrl,
        string fallbackFaviconUrl,
        string fallbackAppleTouchIconUrl,
        string activeMenu)
    {
        string siteName = FirstNonEmpty(shell.Organization.ShortName, shell.Organization.Code, shell.Organization.Name, shell.Congress.Name, shell.Congress.Title) ?? string.Empty;
        string organizationName = FirstNonEmpty(shell.Organization.Name, siteName) ?? string.Empty;
        bool isHomePage = IsHomePage(httpContext, activeMenu);

        string? explicitTitle = FirstNonEmpty(viewData["SeoTitle"] as string, viewData["Title"] as string);
        string pageTitle = isHomePage
            ? FirstNonEmpty(explicitTitle, siteName)!
            : FirstNonEmpty(explicitTitle, siteName)!;

        string title = isHomePage || string.Equals(pageTitle, siteName, StringComparison.OrdinalIgnoreCase)
            ? pageTitle
            : $"{pageTitle} - {siteName}";

        string description = FirstNonEmpty(
            viewData["SeoDescription"] as string,
            isHomePage ? BuildHomeDescription(shell.Organization, shell.Congress, currentCulture) : null,
            shell.Congress.SeoDescription,
            shell.Congress.ShortDescription,
            shell.Congress.Subtitle,
            shell.Congress.WelcomeContent,
            shell.Congress.Name,
            shell.Congress.Title,
            siteName) ?? string.Empty;

        description = NormalizeMetaDescription(description, RecommendedDescriptionLength);

        string keywords = JoinNonEmpty(
            ", ",
            viewData["SeoKeywords"] as string,
            siteName,
            organizationName,
            shell.Congress.Name,
            shell.Congress.Title) ?? string.Empty;

        string brandColor = FirstNonEmpty(shell.Organization.BrandColor, "#0f3b5f")!;
        string canonicalUrl = BuildCanonicalUrl(httpContext, currentCulture, GetDefaultCulture(availableLanguages, currentCulture));
        string faviconLight = FirstNonEmpty(shell.Congress.LogoLightUrl, shell.Organization.LogoLightUrl, logoLightUrl, fallbackFaviconUrl)!;
        string faviconDark = FirstNonEmpty(shell.Congress.LogoDarkUrl, shell.Organization.LogoDarkUrl, logoDarkUrl, faviconLight)!;
        string appleTouchIcon = FirstNonEmpty(shell.Congress.LogoLightUrl, shell.Congress.LogoDarkUrl, shell.Organization.LogoLightUrl, shell.Organization.LogoDarkUrl, fallbackAppleTouchIconUrl)!;
        string socialImage = FirstNonEmpty(shell.Congress.LogoLightUrl, shell.Congress.LogoDarkUrl, shell.Organization.LogoLightUrl, shell.Organization.LogoDarkUrl, logoLightUrl)!;

        Uri origin = GetOrigin(httpContext);
        faviconLight = ToAbsoluteUrl(faviconLight, origin);
        faviconDark = ToAbsoluteUrl(faviconDark, origin);
        appleTouchIcon = ToAbsoluteUrl(appleTouchIcon, origin);
        socialImage = ToAbsoluteUrl(socialImage, origin);

        IReadOnlyList<PortalAlternateLanguageLink> alternateLanguages = BuildAlternateLanguages(httpContext, availableLanguages, currentCulture);
        IReadOnlyList<string> jsonLdBlocks = BuildJsonLdBlocks(
            origin,
            canonicalUrl,
            title,
            description,
            siteName,
            organizationName,
            shell,
            socialImage,
            currentCulture,
            activeMenu);

        return new PortalSeoMetadata
        {
            Title = title,
            Description = description,
            Keywords = keywords,
            SiteName = siteName,
            CanonicalUrl = canonicalUrl,
            BrandColor = brandColor,
            OpenGraphLocale = currentCulture.Replace('-', '_'),
            FaviconLightUrl = faviconLight,
            FaviconDarkUrl = faviconDark,
            AppleTouchIconUrl = appleTouchIcon,
            SocialImageUrl = socialImage,
            AlternateLanguages = alternateLanguages,
            JsonLdBlocks = jsonLdBlocks
        };
    }

    public static string BuildHomeDescription(
        PublicOrganizationResponse organization,
        PublicCongressSummaryResponse congress,
        string currentCulture)
    {
        return FirstNonEmpty(
            congress.SeoDescription,
            congress.ShortDescription,
            congress.Subtitle,
            congress.WelcomeContent,
            congress.Name,
            congress.Title,
            organization.Name,
            organization.ShortName,
            organization.Code) ?? string.Empty;
    }


    public static string BuildDescriptionFromContent(string? title, string? content)
    {
        string normalizedContent = !string.IsNullOrWhiteSpace(content) ? NormalizeMetaDescription(content, RecommendedDescriptionLength) : string.Empty;
        if (!string.IsNullOrWhiteSpace(normalizedContent))
        {
            return normalizedContent;
        }

        return title ?? string.Empty;
    }

    public static string BuildSeoFriendlyCultureUrl(HttpContext httpContext, string culture, IReadOnlyCollection<PublicLanguageResponse> availableLanguages)
    {
        string defaultCulture = GetDefaultCulture(availableLanguages, culture);
        return BuildCanonicalUrl(httpContext, culture, defaultCulture);
    }

    public static string BuildLanguageSwitchUrl(HttpContext httpContext, string culture)
    {
        HttpRequest request = httpContext.Request;
        string path = request.Path.HasValue ? request.Path.Value! : "/";

        Dictionary<string, string?> query = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> item in request.Query)
        {
            if (string.Equals(item.Key, "culture", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string value = item.Value.ToString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                query[item.Key] = value;
            }
        }

        query["culture"] = string.IsNullOrWhiteSpace(culture) ? "tr-TR" : culture.Trim();

        return QueryHelpers.AddQueryString(path, query);
    }

    public static string BuildCultureQuery(string culture, IReadOnlyCollection<PublicLanguageResponse> availableLanguages)
    {
        string defaultCulture = GetDefaultCulture(availableLanguages, culture);
        return string.Equals(culture, defaultCulture, StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : $"?culture={Uri.EscapeDataString(culture)}";
    }

    private static IReadOnlyList<string> BuildJsonLdBlocks(
        Uri origin,
        string canonicalUrl,
        string title,
        string description,
        string siteName,
        string organizationName,
        PublicSiteBootstrapResponse shell,
        string socialImage,
        string currentCulture,
        string activeMenu)
    {
        List<string> blocks = new();

        Dictionary<string, object?> webSite = new()
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "WebSite",
            ["@id"] = $"{origin}#website",
            ["url"] = origin.ToString(),
            ["name"] = siteName,
            ["alternateName"] = !string.Equals(siteName, organizationName, StringComparison.OrdinalIgnoreCase) ? organizationName : null,
            ["inLanguage"] = currentCulture,
            ["publisher"] = new Dictionary<string, object?> { ["@id"] = $"{origin}#organization" }
        };
        blocks.Add(SerializeJsonLd(webSite));

        Dictionary<string, object?> organization = new()
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Organization",
            ["@id"] = $"{origin}#organization",
            ["name"] = organizationName,
            ["alternateName"] = siteName,
            ["url"] = origin.ToString(),
            ["logo"] = socialImage,
            ["sameAs"] = !string.IsNullOrWhiteSpace(shell.Organization.WebsiteUrl) ? new[] { shell.Organization.WebsiteUrl } : null
        };
        blocks.Add(SerializeJsonLd(organization));

        if (!string.IsNullOrWhiteSpace(shell.Congress.Name) || !string.IsNullOrWhiteSpace(shell.Congress.Title))
        {
            Dictionary<string, object?> eventSchema = new()
            {
                ["@context"] = "https://schema.org",
                ["@type"] = "Event",
                ["@id"] = $"{canonicalUrl}#event",
                ["name"] = FirstNonEmpty(shell.Congress.Name, shell.Congress.Title),
                ["description"] = description,
                ["url"] = canonicalUrl,
                ["image"] = socialImage,
                ["startDate"] = ToIsoDate(shell.Congress.StartDate),
                ["endDate"] = ToIsoDate(shell.Congress.EndDate),
                ["eventStatus"] = "https://schema.org/EventScheduled",
                ["eventAttendanceMode"] = "https://schema.org/OfflineEventAttendanceMode",
                ["organizer"] = new Dictionary<string, object?>
                {
                    ["@type"] = "Organization",
                    ["name"] = organizationName,
                    ["url"] = origin.ToString()
                },
                ["location"] = BuildLocationSchema(shell.Congress)
            };
            blocks.Add(SerializeJsonLd(eventSchema));
        }

        Dictionary<string, object?> webPage = new()
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "WebPage",
            ["@id"] = $"{canonicalUrl}#webpage",
            ["url"] = canonicalUrl,
            ["name"] = title,
            ["description"] = description,
            ["isPartOf"] = new Dictionary<string, object?> { ["@id"] = $"{origin}#website" },
            ["about"] = new Dictionary<string, object?> { ["@id"] = $"{canonicalUrl}#event" },
            ["inLanguage"] = currentCulture
        };
        blocks.Add(SerializeJsonLd(webPage));

        Dictionary<string, object?>? breadcrumb = BuildBreadcrumbSchema(origin, canonicalUrl, siteName, title, activeMenu);
        if (breadcrumb is not null)
        {
            blocks.Add(SerializeJsonLd(breadcrumb));
        }

        return blocks;
    }

    private static Dictionary<string, object?>? BuildBreadcrumbSchema(Uri origin, string canonicalUrl, string siteName, string title, string activeMenu)
    {
        if (string.Equals(activeMenu, "home", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "BreadcrumbList",
            ["itemListElement"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["@type"] = "ListItem",
                    ["position"] = 1,
                    ["name"] = siteName,
                    ["item"] = origin.ToString()
                },
                new Dictionary<string, object?>
                {
                    ["@type"] = "ListItem",
                    ["position"] = 2,
                    ["name"] = title,
                    ["item"] = canonicalUrl
                }
            }
        };
    }

    private static Dictionary<string, object?>? BuildLocationSchema(PublicCongressSummaryResponse congress)
    {
        string? locationName = FirstNonEmpty(congress.VenueName, congress.LocationText, JoinNonEmpty(" / ", congress.CityName, congress.CountryName));
        if (string.IsNullOrWhiteSpace(locationName))
        {
            return null;
        }

        string? addressText = FirstNonEmpty(congress.LocationText, JoinNonEmpty(" / ", congress.CityName, congress.CountryName), congress.VenueName);

        return new Dictionary<string, object?>
        {
            ["@type"] = "Place",
            ["name"] = locationName,
            ["address"] = new Dictionary<string, object?>
            {
                ["@type"] = "PostalAddress",
                ["addressLocality"] = congress.CityName,
                ["addressRegion"] = congress.StateName,
                ["addressCountry"] = congress.CountryName,
                ["streetAddress"] = addressText
            }
        };
    }

    private static IReadOnlyList<PortalAlternateLanguageLink> BuildAlternateLanguages(HttpContext httpContext, IReadOnlyCollection<PublicLanguageResponse> availableLanguages, string currentCulture)
    {
        if (availableLanguages.Count == 0)
        {
            return Array.Empty<PortalAlternateLanguageLink>();
        }

        string defaultCulture = GetDefaultCulture(availableLanguages, currentCulture);
        List<PortalAlternateLanguageLink> links = availableLanguages
            .Where(language => !string.IsNullOrWhiteSpace(language.Culture))
            .OrderByDescending(language => language.IsDefault)
            .ThenBy(language => language.Order)
            .Select(language => new PortalAlternateLanguageLink
            {
                Hreflang = ToHreflang(language.Culture),
                Url = BuildCanonicalUrl(httpContext, language.Culture, defaultCulture)
            })
            .ToList();

        links.Add(new PortalAlternateLanguageLink
        {
            Hreflang = "x-default",
            Url = BuildCanonicalUrl(httpContext, defaultCulture, defaultCulture)
        });

        return links;
    }

    private static string BuildCanonicalUrl(HttpContext httpContext, string culture, string defaultCulture)
    {
        HttpRequest request = httpContext.Request;
        string scheme = request.Scheme;
        HostString host = request.Host;
        PathString pathBase = request.PathBase;
        PathString path = request.Path.HasValue ? request.Path : new PathString("/");

        string baseUrl = $"{scheme}://{host}{pathBase}{path}";
        Dictionary<string, string?> query = new(StringComparer.OrdinalIgnoreCase);

        string? documentType = request.Query["type"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(documentType))
        {
            query["type"] = documentType.Trim();
        }

        if (!string.Equals(culture, defaultCulture, StringComparison.OrdinalIgnoreCase))
        {
            query["culture"] = culture;
        }

        return query.Count == 0 ? baseUrl : QueryHelpers.AddQueryString(baseUrl, query);
    }

    private static string NormalizeMetaDescription(string value, int maxLength)
    {
        string normalized = CleanText(value);
        if (normalized.Length <= maxLength)
        {
            return normalized;
        }

        int cutIndex = normalized.LastIndexOf(' ', Math.Min(maxLength, normalized.Length - 1));
        if (cutIndex < 80)
        {
            cutIndex = maxLength;
        }

        return normalized[..cutIndex].TrimEnd(' ', ',', '.', ';', ':') + "...";
    }

    private static string CleanText(string value)
    {
        string decoded = WebUtility.HtmlDecode(value);
        string withoutTags = HtmlTagRegex().Replace(decoded, " ");
        return WhiteSpaceRegex().Replace(withoutTags, " ").Trim();
    }

    private static string? BuildDateRangeText(DateTime? startDate, DateTime? endDate, string currentCulture)
    {
        if (startDate is null && endDate is null)
        {
            return null;
        }

        CultureInfo culture = CreateCulture(currentCulture);
        DateTime start = startDate ?? endDate!.Value;
        DateTime end = endDate ?? startDate!.Value;

        if (start.Date == end.Date)
        {
            return start.ToString("d MMMM yyyy", culture);
        }

        if (start.Month == end.Month && start.Year == end.Year)
        {
            return $"{start:dd} - {end.ToString("dd MMMM yyyy", culture)}";
        }

        return $"{start.ToString("d MMMM yyyy", culture)} - {end.ToString("d MMMM yyyy", culture)}";
    }

    private static CultureInfo CreateCulture(string currentCulture)
    {
        try
        {
            return CultureInfo.GetCultureInfo(currentCulture);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.GetCultureInfo("tr-TR");
        }
    }

    private static Uri GetOrigin(HttpContext httpContext)
    {
        HttpRequest request = httpContext.Request;
        return new Uri($"{request.Scheme}://{request.Host}/");
    }

    private static string ToAbsoluteUrl(string url, Uri origin)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return origin.ToString();
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out Uri? absoluteUri))
        {
            return absoluteUri.ToString();
        }

        return new Uri(origin, url.TrimStart('/')).ToString();
    }

    private static string GetDefaultCulture(IReadOnlyCollection<PublicLanguageResponse> availableLanguages, string currentCulture)
    {
        return availableLanguages.FirstOrDefault(language => language.IsDefault)?.Culture
            ?? availableLanguages.FirstOrDefault()?.Culture
            ?? currentCulture;
    }

    private static string ToHreflang(string culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return "tr";
        }

        string normalized = culture.Trim().Replace('_', '-');
        string[] parts = normalized.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? normalized.ToLowerInvariant() : parts[0].ToLowerInvariant();
    }

    private static string? ToIsoDate(DateTime? value)
    {
        return value?.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static bool IsHomePage(HttpContext httpContext, string activeMenu)
    {
        return string.Equals(activeMenu, "home", StringComparison.OrdinalIgnoreCase)
            || !httpContext.Request.Path.HasValue
            || string.Equals(httpContext.Request.Path.Value, "/", StringComparison.Ordinal);
    }

    private static string? JoinNonEmpty(string separator, params string?[] values)
    {
        string[] nonEmptyValues = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .ToArray();

        return nonEmptyValues.Length == 0 ? null : string.Join(separator, nonEmptyValues);
    }

    private static string SerializeJsonLd(Dictionary<string, object?> value)
    {
        return JsonSerializer.Serialize(RemoveNullValues(value), JsonLdSerializerOptions);
    }

    private static object? RemoveNullValues(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is Dictionary<string, object?> dictionary)
        {
            return dictionary
                .Where(pair => pair.Value is not null)
                .ToDictionary(pair => pair.Key, pair => RemoveNullValues(pair.Value), StringComparer.Ordinal);
        }

        if (value is IEnumerable<object?> sequence && value is not string)
        {
            return sequence
                .Select(RemoveNullValues)
                .Where(item => item is not null)
                .ToArray();
        }

        return value;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (string? value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    [GeneratedRegex("<.*?>", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex("\\s+", RegexOptions.Compiled)]
    private static partial Regex WhiteSpaceRegex();
}
