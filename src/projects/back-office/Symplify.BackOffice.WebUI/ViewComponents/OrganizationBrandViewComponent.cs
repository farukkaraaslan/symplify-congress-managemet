using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Domain.Identity;
using Symplify.BackOffice.Persistence.Contexts;
using Symplify.BackOffice.WebUI.Models.Navbar;

namespace Symplify.BackOffice.WebUI.ViewComponents;

public sealed class OrganizationBrandViewComponent : ViewComponent
{
    private const string DefaultCulture = "tr-TR";
    private const string DefaultLogoUrl = "/assets/images/logo.png";

    private readonly BackOfficeDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _configuration;

    public OrganizationBrandViewComponent(
        BackOfficeDbContext context,
        UserManager<AppUser> userManager,
        IConfiguration configuration)
    {
        _context = context;
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        string culture = ResolveCurrentCulture();
        string homeUrl = Url.Action("Index", "Home", new { culture }) ?? $"/{culture}/Home/Index";

        OrganizationBrandViewModel model = new()
        {
            HomeUrl = homeUrl,
            LogoUrl = DefaultLogoUrl,
            AltText = "Symplify"
        };

        Guid? organizationId = ResolveCurrentOrganizationId(UserClaimsPrincipal);

        if (!organizationId.HasValue || organizationId.Value == Guid.Empty)
        {
            AppUser? currentUser = await _userManager.GetUserAsync(UserClaimsPrincipal);
            if (currentUser is not null)
                organizationId = await ResolveLatestUserOrganizationIdAsync(currentUser.Id);
        }

        if (!organizationId.HasValue || organizationId.Value == Guid.Empty)
            return View(model);

        var organization = await _context.Organizations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(item =>
                item.Id == organizationId.Value &&
                item.IsActive &&
                item.DeletedDate == null)
            .Select(item => new
            {
                item.Name,
                item.LogoLightPath,
                item.LogoDarkPath
            })
            .FirstOrDefaultAsync();

        if (organization is null)
            return View(model);

        model.AltText = string.IsNullOrWhiteSpace(organization.Name)
            ? model.AltText
            : organization.Name.Trim();

        model.LogoUrl = ResolveLogoUrl(organization.LogoLightPath)
            ?? ResolveLogoUrl(organization.LogoDarkPath)
            ?? model.LogoUrl;

        return View(model);
    }

    private ClaimsPrincipal UserClaimsPrincipal => HttpContext?.User ?? new ClaimsPrincipal();

    private async Task<Guid?> ResolveLatestUserOrganizationIdAsync(Guid userId)
    {
        return await _context.OrganizationUsers
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(item =>
                item.UserId == userId &&
                item.IsActive &&
                item.DeletedDate == null)
            .OrderByDescending(item => item.CreatedDate)
            .ThenBy(item => item.Id)
            .Select(item => (Guid?)item.OrganizationId)
            .FirstOrDefaultAsync();
    }

    private string? ResolveLogoUrl(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        string normalizedPath = path.Trim().Replace('\\', '/');

        if (normalizedPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.StartsWith("/", StringComparison.Ordinal))
        {
            return normalizedPath;
        }

        if (normalizedPath.StartsWith("~/", StringComparison.Ordinal))
            return "/" + normalizedPath[2..].TrimStart('/');

        string? bucketName = _configuration["ObjectStorage:Buckets:CongressImages"];

        if (string.IsNullOrWhiteSpace(bucketName))
            return null;

        string encodedBucketName = Uri.EscapeDataString(bucketName.Trim().Trim('/'));
        string encodedObjectName = string.Join(
            '/',
            normalizedPath
                .Trim('/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Uri.EscapeDataString));

        return string.IsNullOrWhiteSpace(encodedObjectName)
            ? null
            : $"/public-assets/{encodedBucketName}/{encodedObjectName}";
    }

    private string ResolveCurrentCulture()
    {
        string? routeCulture = HttpContext?.Request.RouteValues["culture"]?.ToString();

        if (!string.IsNullOrWhiteSpace(routeCulture))
            return NormalizeCulture(routeCulture);

        string? pathCulture = HttpContext?.Request.Path.Value?
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

    private static Guid? ResolveCurrentOrganizationId(ClaimsPrincipal principal)
    {
        string? organizationId = principal.FindFirstValue("OrganizationId");
        return Guid.TryParse(organizationId, out Guid parsedOrganizationId)
            ? parsedOrganizationId
            : null;
    }
}
