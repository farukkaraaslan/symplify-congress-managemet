using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.WebUI.Services.Localization;

public sealed class BackOfficeCultureResolver : IBackOfficeCultureResolver
{
    private readonly BackOfficeDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BackOfficeCultureResolver(
        BackOfficeDbContext context,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<string> ResolveCurrentCultureAsync(
        CancellationToken cancellationToken = default)
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

        return await NormalizeCultureAsync(
            formCulture
            ?? queryCulture
            ?? headerCulture
            ?? routeCulture
            ?? pathCulture,
            cancellationToken);
    }

    public async Task<Guid?> ResolveCurrentLanguageIdAsync(
        CancellationToken cancellationToken = default)
    {
        string culture = await ResolveCurrentCultureAsync(cancellationToken);

        return await _context.Languages
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(language => language.IsActive)
            .Where(language => language.Culture == culture)
            .Select(language => (Guid?)language.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<string> NormalizeCultureAsync(
        string? culture,
        CancellationToken cancellationToken = default)
    {
        string? requestedCulture = culture?.Trim();

        if (!string.IsNullOrWhiteSpace(requestedCulture))
        {
            string? matchedCulture = await _context.Languages
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(language => language.IsActive)
                .Where(language =>
                    language.Culture.ToLower() == requestedCulture.ToLower() ||
                    language.TwoLetterIsoCode.ToLower() == requestedCulture.ToLower())
                .OrderBy(language => language.Order)
                .Select(language => language.Culture)
                .FirstOrDefaultAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(matchedCulture))
                return matchedCulture;
        }

        string? defaultCulture = await _context.Languages
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(language => language.IsActive && language.IsDefault)
            .OrderBy(language => language.Order)
            .Select(language => language.Culture)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(defaultCulture))
            return defaultCulture;

        string? firstActiveCulture = await _context.Languages
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(language => language.IsActive)
            .OrderBy(language => language.Order)
            .Select(language => language.Culture)
            .FirstOrDefaultAsync(cancellationToken);

        return firstActiveCulture ?? "tr-TR";
    }
}