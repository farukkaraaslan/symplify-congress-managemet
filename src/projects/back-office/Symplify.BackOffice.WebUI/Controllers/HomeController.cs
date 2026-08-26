using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Identity;
using CongressEntity = Symplify.BackOffice.Domain.Congress.Congress;
using Symplify.BackOffice.Persistence.Contexts;
using Symplify.BackOffice.WebUI.Models;
using Symplify.BackOffice.WebUI.Models.Home;
using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;

namespace Symplify.BackOffice.WebUI.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly BackOfficeDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public HomeController(
            BackOfficeDbContext context,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            AppUser? currentUser = await _userManager.GetUserAsync(User);
            string culture = ResolveCurrentCulture();
            Guid? requestedLanguageId = await ResolveLanguageIdAsync(culture, cancellationToken);
            Guid? defaultLanguageId = await ResolveDefaultLanguageIdAsync(cancellationToken);

            var model = new HomeIndexViewModel
            {
                DisplayName = ResolveDisplayName(currentUser),
                ActiveCongress = currentUser is null
                    ? null
                    : await ResolveActiveCongressAsync(currentUser.Id, requestedLanguageId, defaultLanguageId, cancellationToken)
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private string ResolveDisplayName(AppUser? currentUser)
        {
            if (currentUser is not null)
            {
                string fullName = $"{currentUser.Name} {currentUser.Surname}".Trim();

                if (!string.IsNullOrWhiteSpace(fullName))
                    return fullName;
            }

            return User?.Identity?.Name ?? string.Empty;
        }

        private async Task<ActiveCongressSummaryViewModel?> ResolveActiveCongressAsync(
            Guid userId,
            Guid? requestedLanguageId,
            Guid? defaultLanguageId,
            CancellationToken cancellationToken)
        {
            Guid? organizationId = ResolveCurrentOrganizationId(User);

            var organizationUserQuery = _context.OrganizationUsers
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(item =>
                    item.UserId == userId &&
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
                ActiveCongressSummaryViewModel? defaultCongress = await ResolveCongressSummaryAsync(
                    organizationUser.OrganizationId,
                    organizationUser.DefaultCongressId.Value,
                    requestedLanguageId,
                    defaultLanguageId,
                    cancellationToken);

                if (defaultCongress is not null)
                    return defaultCongress;
            }

            CongressEntity? congress = await _context.Congresses
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(item => item.Translations)
                .Where(item =>
                    item.OrganizationId == organizationUser.OrganizationId &&
                    item.Status == CongressStatus.Published &&
                    item.DeletedDate == null)
                .OrderByDescending(item => item.StartDate)
                .ThenByDescending(item => item.CreatedDate)
                .FirstOrDefaultAsync(cancellationToken);

            return congress is null
                ? null
                : MapCongressSummary(congress, requestedLanguageId, defaultLanguageId);
        }

        private async Task<ActiveCongressSummaryViewModel?> ResolveCongressSummaryAsync(
            Guid organizationId,
            Guid congressId,
            Guid? requestedLanguageId,
            Guid? defaultLanguageId,
            CancellationToken cancellationToken)
        {
            CongressEntity? congress = await _context.Congresses
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(item => item.Translations)
                .Where(item =>
                    item.Id == congressId &&
                    item.OrganizationId == organizationId &&
                    item.Status == CongressStatus.Published &&
                    item.DeletedDate == null)
                .FirstOrDefaultAsync(cancellationToken);

            return congress is null
                ? null
                : MapCongressSummary(congress, requestedLanguageId, defaultLanguageId);
        }

        private static ActiveCongressSummaryViewModel MapCongressSummary(
            CongressEntity congress,
            Guid? requestedLanguageId,
            Guid? defaultLanguageId)
        {
            return new ActiveCongressSummaryViewModel
            {
                Id = congress.Id,
                Name = ResolveCongressDisplayName(
                    congress.Name,
                    congress.Code,
                    congress.Translations
                        .Where(translation => translation.DeletedDate == null)
                        .Select(translation => new CongressTranslationNameProjection
                        {
                            LanguageId = translation.LanguageId,
                            Title = translation.Title
                        }),
                    requestedLanguageId,
                    defaultLanguageId),
                StartDate = congress.StartDate,
                EndDate = congress.EndDate,
                VenueName = congress.VenueName
            };
        }

        private static string ResolveCongressDisplayName(
            string? fallbackName,
            string? fallbackCode,
            IEnumerable<CongressTranslationNameProjection> translations,
            Guid? requestedLanguageId,
            Guid? defaultLanguageId)
        {
            if (requestedLanguageId.HasValue)
            {
                string? requestedTitle = translations
                    .FirstOrDefault(translation => translation.LanguageId == requestedLanguageId.Value)?.Title;

                if (!string.IsNullOrWhiteSpace(requestedTitle))
                    return requestedTitle.Trim();
            }

            if (defaultLanguageId.HasValue)
            {
                string? defaultTitle = translations
                    .FirstOrDefault(translation => translation.LanguageId == defaultLanguageId.Value)?.Title;

                if (!string.IsNullOrWhiteSpace(defaultTitle))
                    return defaultTitle.Trim();
            }

            if (!string.IsNullOrWhiteSpace(fallbackName))
                return fallbackName.Trim();

            return string.IsNullOrWhiteSpace(fallbackCode) ? "-" : fallbackCode.Trim();
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

        private string ResolveCurrentCulture()
        {
            string? routeCulture = RouteData.Values["culture"]?.ToString();

            if (!string.IsNullOrWhiteSpace(routeCulture))
                return NormalizeCulture(routeCulture);

            string? pathCulture = HttpContext.Request.Path.Value?
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();

            return NormalizeCulture(pathCulture);
        }

        private static string NormalizeCulture(string? culture)
        {
            if (string.IsNullOrWhiteSpace(culture))
                return "tr-TR";

            return culture.Trim().Replace("_", "-").ToLowerInvariant() switch
            {
                "tr" => "tr-TR",
                "tr-tr" => "tr-TR",
                "en" => "en-US",
                "en-us" => "en-US",
                _ => culture
            };
        }

        private static Guid? ResolveCurrentOrganizationId(ClaimsPrincipal principal)
        {
            string? organizationId = principal.FindFirstValue("OrganizationId");
            return Guid.TryParse(organizationId, out Guid parsedOrganizationId) ? parsedOrganizationId : null;
        }

        private sealed class CongressTranslationNameProjection
        {
            public Guid LanguageId { get; init; }

            public string? Title { get; init; }
        }
    }
}
