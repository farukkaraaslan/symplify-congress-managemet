using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Symplify.Portal.WebUI.Helpers;
using Symplify.Portal.WebUI.Models.PublicSite;
using Symplify.Portal.WebUI.Services.PublicSite;

namespace Symplify.Portal.WebUI.Controllers;

public sealed class SeoController : Controller
{
    private static readonly string[] StaticPaths =
    {
        "/",
        "/sections",
        "/boards",
        "/payment",
        "/contents",
        "/documents",
        "/contact"
    };

    private readonly IPublicSiteService _publicSiteService;
    private readonly IPortalCultureService _cultureService;
    private readonly ILogger<SeoController> _logger;

    public SeoController(
        IPublicSiteService publicSiteService,
        IPortalCultureService cultureService,
        ILogger<SeoController> logger)
    {
        _publicSiteService = publicSiteService;
        _cultureService = cultureService;
        _logger = logger;
    }

    [HttpGet("robots.txt")]
    public IActionResult Robots()
    {
        string origin = GetOrigin();
        string content = $"""
                      User-agent: *
                      Allow: /

                      Sitemap: {origin}/sitemap.xml
                      """;

        return Content(content, "text/plain; charset=utf-8");
    }

    [HttpGet("sitemap.xml")]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken)
    {
        try
        {
            string culture = _cultureService.GetCurrentCulture();
            PublicSiteBootstrapResponse shell = await _publicSiteService.GetShellAsync(cancellationToken);
            PublicSectionsResponse sections = await _publicSiteService.GetSectionsAsync(cancellationToken);
            IReadOnlyList<string> documentTypeNames = await _publicSiteService.GetDocumentTypeNamesAsync(cancellationToken);

            string defaultCulture = shell.Languages.FirstOrDefault(language => language.IsDefault)?.Culture
                ?? shell.Languages.FirstOrDefault()?.Culture
                ?? culture;

            List<SitemapEntry> entries = new();

            foreach (string path in StaticPaths)
            {
                entries.Add(new SitemapEntry(BuildAbsoluteUrl(path, culture, defaultCulture), "weekly", 0.8m));
            }

            foreach (PublicSectionResponse section in sections.Sections
                         .Where(section => !PortalSectionClassifier.IsPaymentSection(section))
                         .Where(section => !string.IsNullOrWhiteSpace(section.BindingKey)))
            {
                entries.Add(new SitemapEntry(BuildAbsoluteUrl($"/sections/{Uri.EscapeDataString(section.BindingKey)}", culture, defaultCulture), "monthly", 0.7m));
            }

            foreach (string documentTypeName in documentTypeNames.Where(name => !string.IsNullOrWhiteSpace(name)))
            {
                entries.Add(new SitemapEntry(BuildAbsoluteUrl("/documents", culture, defaultCulture, new Dictionary<string, string>
                {
                    ["type"] = documentTypeName
                }), "monthly", 0.6m));
            }

            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            XDocument document = new(
                new XElement(ns + "urlset",
                    entries
                        .GroupBy(entry => entry.Location, StringComparer.OrdinalIgnoreCase)
                        .Select(group => group.First())
                        .Select(entry => new XElement(ns + "url",
                            new XElement(ns + "loc", entry.Location),
                            new XElement(ns + "lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd")),
                            new XElement(ns + "changefreq", entry.ChangeFrequency),
                            new XElement(ns + "priority", entry.Priority.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture))))));

            return Content(document.ToString(SaveOptions.DisableFormatting), "application/xml; charset=utf-8");
        }
        catch (Exception exception) when (exception is PublicSiteApiException or HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(exception, "Sitemap could not be generated from Public API data. Returning static sitemap.");

            string defaultCulture = _cultureService.GetCurrentCulture();
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            XDocument document = new(
                new XElement(ns + "urlset",
                    StaticPaths.Select(path => new XElement(ns + "url",
                        new XElement(ns + "loc", BuildAbsoluteUrl(path, defaultCulture, defaultCulture)),
                        new XElement(ns + "lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd"))))));

            return Content(document.ToString(SaveOptions.DisableFormatting), "application/xml; charset=utf-8");
        }
    }

    private string BuildAbsoluteUrl(string path, string culture, string defaultCulture, IReadOnlyDictionary<string, string>? queryValues = null)
    {
        string url = GetOrigin() + (path.StartsWith('/') ? path : "/" + path);
        Dictionary<string, string?> query = new(StringComparer.OrdinalIgnoreCase);

        if (!string.Equals(culture, defaultCulture, StringComparison.OrdinalIgnoreCase))
        {
            query["culture"] = culture;
        }

        if (queryValues is not null)
        {
            foreach (KeyValuePair<string, string> queryValue in queryValues)
            {
                if (!string.IsNullOrWhiteSpace(queryValue.Value))
                {
                    query[queryValue.Key] = queryValue.Value;
                }
            }
        }

        if (query.Count == 0)
        {
            return url;
        }

        string queryString = string.Join("&", query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value ?? string.Empty)}"));
        return $"{url}?{queryString}";
    }

    private string GetOrigin()
    {
        return $"{Request.Scheme}://{Request.Host}".TrimEnd('/');
    }

    private sealed record SitemapEntry(string Location, string ChangeFrequency, decimal Priority);
}
