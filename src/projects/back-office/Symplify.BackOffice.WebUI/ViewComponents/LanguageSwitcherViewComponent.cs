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

    public async Task<IViewComponentResult> InvokeAsync(string displayMode = "Default")
    {
        ViewData["DisplayMode"] = displayMode;
        string currentCulture = ResolveCurrentCulture();

        var languageRows = await _context.Languages
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(language => language.IsActive)
            .OrderByDescending(language => language.IsDefault)
            .ThenBy(language => language.Order)
            .ThenBy(language => language.Name)
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
                string twoLetterIsoCode = ResolveTwoLetterIsoCode(language.TwoLetterIsoCode, normalizedCulture);

                return new LanguageSwitcherItemViewModel
                {
                    Id = language.Id,
                    Name = language.Name,
                    Culture = normalizedCulture,
                    TwoLetterIsoCode = twoLetterIsoCode,
                    FlagIconPath = ResolveFlagIconPath(normalizedCulture, twoLetterIsoCode),
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

        string normalizedValue = value.Trim().Replace("_", "-").ToLowerInvariant();

        return normalizedValue is "tr" or "tr-tr" or "en" or "en-us";
    }

    private static string NormalizeCulture(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
            return DefaultCulture;

        return culture.Trim().Replace("_", "-").ToLowerInvariant() switch
        {
            "tr" => "tr-TR",
            "tr-tr" => "tr-TR",
            "en" => "en-US",
            "en-us" => "en-US",
            _ => culture
        };
    }

    private static string ResolveTwoLetterIsoCode(string? twoLetterIsoCode, string culture)
    {
        if (!string.IsNullOrWhiteSpace(twoLetterIsoCode))
            return twoLetterIsoCode.Trim().ToLowerInvariant();

        string normalizedCulture = NormalizeCulture(culture);

        if (normalizedCulture.Length >= 2)
            return normalizedCulture[..2].ToLowerInvariant();

        return "tr";
    }

    private static string ResolveFlagIconPath(string culture, string twoLetterIsoCode)
    {
        string normalizedCulture = NormalizeCulture(culture).ToLowerInvariant();

        string flagCode = normalizedCulture switch
        {
            "tr-tr" => "tr",
            "en-us" => "gb",
            _ => twoLetterIsoCode.ToLowerInvariant()
        };

        return $"/assets/images/flags/{flagCode}.svg";
    }
}
