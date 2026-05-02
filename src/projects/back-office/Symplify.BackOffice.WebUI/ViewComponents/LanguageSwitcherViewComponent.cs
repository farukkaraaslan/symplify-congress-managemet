using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Persistence.Contexts;
using Symplify.BackOffice.WebUI.Models.LanguageSwitcher;

namespace Symplify.BackOffice.WebUI.ViewComponents;

public sealed class LanguageSwitcherViewComponent : ViewComponent
{
    private const string DefaultCulture = "tr-TR";

    private readonly BackOfficeDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LanguageSwitcherViewComponent(
        BackOfficeDbContext context,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        string currentCulture = ResolveCurrentCulture();

        var languageRows = await _context.Languages
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(language => language.IsActive)
            .OrderBy(language => language.Order)
            .Select(language => new
            {
                language.Id,
                language.Name,
                language.Culture,
                language.TwoLetterIsoCode,
                language.IsDefault
            })
            .ToListAsync();

        List<LanguageSwitcherItemViewModel> languages = languageRows
            .Select(language =>
            {
                string normalizedCulture = NormalizeCulture(language.Culture);

                return new LanguageSwitcherItemViewModel
                {
                    Id = language.Id,
                    Name = language.Name,
                    Culture = normalizedCulture,
                    TwoLetterIsoCode = language.TwoLetterIsoCode,
                    IsDefault = language.IsDefault,
                    IsCurrent = string.Equals(
                        normalizedCulture,
                        currentCulture,
                        StringComparison.OrdinalIgnoreCase),
                    Url = BuildCultureUrl(normalizedCulture)
                };
            })
            .ToList();

        LanguageSwitcherItemViewModel? currentLanguage = languages
            .FirstOrDefault(language => language.IsCurrent)
            ?? languages.FirstOrDefault(language => language.IsDefault)
            ?? languages.FirstOrDefault();

        LanguageSwitcherViewModel model = new()
        {
            CurrentCulture = currentCulture,
            CurrentLanguage = currentLanguage,
            Languages = languages
        };

        return View(model);
    }

    private string ResolveCurrentCulture()
    {
        HttpContext? httpContext = _httpContextAccessor.HttpContext;

        string? routeCulture = httpContext?.Request.RouteValues["culture"]?.ToString();

        if (!string.IsNullOrWhiteSpace(routeCulture))
            return NormalizeCulture(routeCulture);

        string? pathCulture = httpContext?.Request.Path.Value?
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(pathCulture) && IsCultureSegment(pathCulture))
            return NormalizeCulture(pathCulture);

        return DefaultCulture;
    }

    private string BuildCultureUrl(string targetCulture)
    {
        HttpContext? httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is null)
            return $"/{targetCulture}";

        string path = httpContext.Request.Path.Value ?? "/";
        string queryString = httpContext.Request.QueryString.HasValue
            ? httpContext.Request.QueryString.Value ?? string.Empty
            : string.Empty;

        List<string> segments = path
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        if (segments.Count > 0 && IsCultureSegment(segments[0]))
        {
            segments[0] = targetCulture;
        }
        else
        {
            segments.Insert(0, targetCulture);
        }

        string newPath = "/" + string.Join("/", segments);

        return newPath + queryString;
    }

    private static bool IsCultureSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string normalizedValue = value.Trim().ToLowerInvariant();

        return normalizedValue is "tr" or "tr-tr" or "tr-TR"
            or "en" or "en-us" or "en-US";
    }

    private static string NormalizeCulture(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
            return DefaultCulture;

        return culture.Trim().ToLowerInvariant() switch
        {
            "tr" => "tr-TR",
            "tr-tr" => "tr-TR",
            "en" => "en-US",
            "en-us" => "en-US",
            _ => culture
        };
    }
}