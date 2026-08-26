using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Persistence.Contexts;
using Symplify.BackOffice.WebUI.Models.SubmissionEntry;

namespace Symplify.BackOffice.WebUI.ViewComponents;

public sealed class SubmissionEntryMenuViewComponent : ViewComponent
{
    private const string DefaultCulture = "tr-TR";

    private readonly BackOfficeDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SubmissionEntryMenuViewComponent(
        BackOfficeDbContext context,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IViewComponentResult> InvokeAsync(string displayMode = "Sidebar")
    {
        HttpContext? httpContext = _httpContextAccessor.HttpContext;

        if (httpContext?.User?.Identity?.IsAuthenticated != true)
            return View(new SubmissionEntryMenuViewModel { DisplayMode = displayMode });

        Guid? congressId = await ResolveCurrentCongressIdAsync(httpContext, HttpContext.RequestAborted);

        if (!congressId.HasValue)
            return View(new SubmissionEntryMenuViewModel { DisplayMode = displayMode });

        string culture = ResolveCurrentCulture(httpContext);
        Guid? languageId = await ResolveLanguageIdAsync(culture, HttpContext.RequestAborted);
        Guid? defaultLanguageId = await ResolveDefaultLanguageIdAsync(HttpContext.RequestAborted);

        var rows = await _context.CongressSubmissionTypes
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(relation => relation.SubmissionType)
            .Where(relation =>
                relation.CongressId == congressId.Value &&
                relation.IsActive &&
                relation.DeletedDate == null &&
                relation.SubmissionType.IsActive &&
                relation.SubmissionType.DeletedDate == null)
            .OrderBy(relation => relation.Order <= 0 ? int.MaxValue : relation.Order)
            .ThenBy(relation => relation.Id)
            .Select(relation => new
            {
                relation.SubmissionTypeId,
                relation.Order,
                relation.SubmissionType.Code,
                relation.SubmissionType.FormProfile
            })
            .ToListAsync(HttpContext.RequestAborted);

        List<Guid> typeIds = rows.Select(row => row.SubmissionTypeId).ToList();

        var translations = await _context.SubmissionTypeTranslations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(translation =>
                typeIds.Contains(translation.SubmissionTypeId) &&
                translation.DeletedDate == null)
            .Select(translation => new SubmissionTypeTranslationRow(
                translation.SubmissionTypeId,
                translation.LanguageId,
                translation.Name))
            .ToListAsync(HttpContext.RequestAborted);

        IReadOnlyList<SubmissionEntryMenuItemViewModel> items = rows
            .Select(row => new SubmissionEntryMenuItemViewModel
            {
                SubmissionTypeId = row.SubmissionTypeId,
                Code = row.Code ?? string.Empty,
                Text = ResolveTypeName(row.SubmissionTypeId, row.Code, translations, languageId, defaultLanguageId),
                FormProfile = row.FormProfile,
                Url = BuildCreateUrl(culture, row.SubmissionTypeId, row.FormProfile),
                Icon = ResolveIcon(row.FormProfile, row.Code),
                IsActive = IsCurrentEntryUrl(httpContext, BuildCreateUrl(culture, row.SubmissionTypeId, row.FormProfile))
            })
            .ToList();

        return View(new SubmissionEntryMenuViewModel
        {
            DisplayMode = displayMode,
            Items = items
        });
    }

    private async Task<Guid?> ResolveCurrentCongressIdAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        Guid? currentUserId = ResolveCurrentUserId(httpContext.User);
        if (!currentUserId.HasValue)
            return null;

        Guid? organizationId = ResolveCurrentOrganizationId(httpContext.User);

        var organizationUserQuery = _context.OrganizationUsers
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(item =>
                item.UserId == currentUserId.Value &&
                item.IsActive &&
                item.DeletedDate == null);

        if (organizationId.HasValue && organizationId.Value != Guid.Empty)
            organizationUserQuery = organizationUserQuery.Where(item => item.OrganizationId == organizationId.Value);

        var organizationUser = await organizationUserQuery
            .OrderByDescending(item => item.CreatedDate)
            .ThenBy(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (organizationUser is null)
            return null;

        if (organizationUser.DefaultCongressId.HasValue)
        {
            Guid defaultCongressId = organizationUser.DefaultCongressId.Value;

            bool defaultCongressIsAvailable = await _context.Congresses
                .AsNoTracking()
                .IgnoreQueryFilters()
                .AnyAsync(item =>
                    item.Id == defaultCongressId &&
                    item.OrganizationId == organizationUser.OrganizationId &&
                    item.Status == CongressStatus.Published &&
                    item.DeletedDate == null,
                    cancellationToken);

            if (defaultCongressIsAvailable)
                return defaultCongressId;
        }

        var congress = await _context.Congresses
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(item =>
                item.OrganizationId == organizationUser.OrganizationId &&
                item.Status == CongressStatus.Published &&
                item.DeletedDate == null)
            .OrderByDescending(item => item.StartDate)
            .ThenByDescending(item => item.CreatedDate)
            .Select(item => new { item.Id })
            .FirstOrDefaultAsync(cancellationToken);

        return congress?.Id;
    }

    private async Task<Guid?> ResolveLanguageIdAsync(string culture, CancellationToken cancellationToken)
    {
        var language = await _context.Languages
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(item => item.Culture == culture && item.DeletedDate == null)
            .Select(item => new { item.Id })
            .FirstOrDefaultAsync(cancellationToken);

        return language?.Id;
    }

    private async Task<Guid?> ResolveDefaultLanguageIdAsync(CancellationToken cancellationToken)
    {
        var language = await _context.Languages
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(item => item.IsDefault && item.DeletedDate == null)
            .Select(item => new { item.Id })
            .FirstOrDefaultAsync(cancellationToken);

        return language?.Id;
    }

    private static string ResolveTypeName(
        Guid submissionTypeId,
        string? code,
        IEnumerable<SubmissionTypeTranslationRow> translations,
        Guid? languageId,
        Guid? defaultLanguageId)
    {
        string? requestedName = languageId.HasValue
            ? translations.FirstOrDefault(translation =>
                translation.SubmissionTypeId == submissionTypeId &&
                translation.LanguageId == languageId.Value)?.Name
            : null;

        if (!string.IsNullOrWhiteSpace(requestedName))
            return requestedName.Trim();

        string? defaultName = defaultLanguageId.HasValue
            ? translations.FirstOrDefault(translation =>
                translation.SubmissionTypeId == submissionTypeId &&
                translation.LanguageId == defaultLanguageId.Value)?.Name
            : null;

        if (!string.IsNullOrWhiteSpace(defaultName))
            return defaultName.Trim();

        return string.IsNullOrWhiteSpace(code) ? submissionTypeId.ToString() : code.Trim();
    }

    private sealed record SubmissionTypeTranslationRow(Guid SubmissionTypeId, Guid LanguageId, string Name);



    private static bool IsCurrentEntryUrl(HttpContext httpContext, string itemUrl)
    {
        string currentPath = httpContext.Request.Path.Value ?? string.Empty;

        // Başvuru tipi linkleri sadece create ekranındayken aktif olmalı.
        // Detail/Edit/Index gibi submission sayfalarında Poster/Sözlü/Sergi linklerinin
        // aktif görünmesi kullanıcıyı yanıltıyor.
        if (!currentPath.Contains("/submissions/create", StringComparison.OrdinalIgnoreCase) &&
            !currentPath.Contains("/exhibition-applications/create", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string current = NormalizePathAndQuery(currentPath, httpContext.Request.QueryString.Value);
        string target = NormalizePathAndQuery(itemUrl);

        return string.Equals(current, target, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePathAndQuery(string? path, string? query)
    {
        string normalizedPath = string.IsNullOrWhiteSpace(path) ? "/" : path.Trim();
        string normalizedQuery = string.IsNullOrWhiteSpace(query) ? string.Empty : query.Trim();

        return normalizedQuery.Length == 0
            ? normalizedPath.TrimEnd('/')
            : $"{normalizedPath.TrimEnd('/')}{normalizedQuery}";
    }

    private static string NormalizePathAndQuery(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        if (Uri.TryCreate(url, UriKind.Absolute, out Uri? absoluteUri))
            return NormalizePathAndQuery(absoluteUri.AbsolutePath, absoluteUri.Query);

        if (Uri.TryCreate(url, UriKind.Relative, out Uri? relativeUri))
        {
            string[] parts = url.Split('?', 2);
            string path = parts[0];
            string query = parts.Length > 1 ? "?" + parts[1] : string.Empty;
            return NormalizePathAndQuery(path, query);
        }

        return url.Trim();
    }

    private string BuildCreateUrl(string culture, Guid submissionTypeId, SubmissionFormProfile formProfile)
    {
        string controller = formProfile == SubmissionFormProfile.ExhibitionApplication
            ? "ExhibitionApplications"
            : "Submissions";

        string fallbackPath = formProfile == SubmissionFormProfile.ExhibitionApplication
            ? "exhibition-applications"
            : "submissions";

        return Url.Action("Create", controller, new { culture, submissionTypeId })
               ?? $"/{culture}/{fallbackPath}/create?submissionTypeId={submissionTypeId:D}";
    }

    private static string ResolveIcon(SubmissionFormProfile formProfile, string? code)
    {
        if (formProfile == SubmissionFormProfile.ExhibitionApplication)
            return "solar:gallery-wide-outline";

        string normalizedCode = NormalizeKindCode(code);
        if (normalizedCode.Contains("POSTER", StringComparison.OrdinalIgnoreCase))
            return "solar:gallery-outline";

        if (normalizedCode.Contains("ORAL", StringComparison.OrdinalIgnoreCase) ||
            normalizedCode.Contains("SOZLU", StringComparison.OrdinalIgnoreCase) ||
            normalizedCode.Contains("SOZEL", StringComparison.OrdinalIgnoreCase))
            return "solar:microphone-2-outline";

        return "solar:document-add-outline";
    }

    private static string NormalizeKindCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim()
            .Replace("İ", "I", StringComparison.Ordinal)
            .Replace("ı", "i", StringComparison.Ordinal)
            .Replace("Ö", "O", StringComparison.Ordinal)
            .Replace("ö", "o", StringComparison.Ordinal)
            .Replace("Ü", "U", StringComparison.Ordinal)
            .Replace("ü", "u", StringComparison.Ordinal)
            .Replace("Ş", "S", StringComparison.Ordinal)
            .Replace("ş", "s", StringComparison.Ordinal)
            .Replace("Ğ", "G", StringComparison.Ordinal)
            .Replace("ğ", "g", StringComparison.Ordinal)
            .Replace("Ç", "C", StringComparison.Ordinal)
            .Replace("ç", "c", StringComparison.Ordinal)
            .ToUpperInvariant();
    }

    private static Guid? ResolveCurrentUserId(ClaimsPrincipal principal)
    {
        string? rawId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(rawId, out Guid userId) ? userId : null;
    }

    private static Guid? ResolveCurrentOrganizationId(ClaimsPrincipal principal)
    {
        string? organizationId = principal.FindFirstValue("OrganizationId");
        return Guid.TryParse(organizationId, out Guid parsedOrganizationId) ? parsedOrganizationId : null;
    }

    private static string ResolveCurrentCulture(HttpContext httpContext)
    {
        string? routeCulture = httpContext.Request.RouteValues["culture"]?.ToString();

        if (!string.IsNullOrWhiteSpace(routeCulture))
            return NormalizeCulture(routeCulture);

        string? pathCulture = httpContext.Request.Path.Value?
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        return NormalizeCulture(pathCulture);
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
}
