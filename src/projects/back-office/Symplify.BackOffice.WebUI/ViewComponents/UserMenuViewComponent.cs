using System.Security.Claims;
using Core.Application.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Domain.Identity;
using Symplify.BackOffice.WebUI.Models.Navbar;

namespace Symplify.BackOffice.WebUI.ViewComponents;

public sealed class UserMenuViewComponent : ViewComponent
{
    private const string DefaultCulture = "tr-TR";

    private readonly UserManager<AppUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ObjectStorageOptions _storageOptions;

    public UserMenuViewComponent(
        UserManager<AppUser> userManager,
        IHttpContextAccessor httpContextAccessor,
        IOptions<ObjectStorageOptions> storageOptions)
    {
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
        _storageOptions = storageOptions.Value;
    }

    public async Task<IViewComponentResult> InvokeAsync(string displayMode = "Default")
    {
        ViewData["DisplayMode"] = displayMode;

        string culture = ResolveCurrentCulture();
        ClaimsPrincipal principal = _httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();

        string displayName = principal.Identity?.Name ?? "Kullanıcı";
        string email = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        string primaryRole = principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value).FirstOrDefault() ?? "Kullanıcı";
        Guid userId = Guid.Empty;
        string? profileImageUrl = null;

        string? userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (Guid.TryParse(userIdValue, out Guid parsedUserId))
        {
            userId = parsedUserId;
            AppUser? user = await _userManager.FindByIdAsync(parsedUserId.ToString());

            if (user is not null)
            {
                displayName = BuildDisplayName(user, displayName);
                email = user.Email ?? email;
                profileImageUrl = BuildProfileImageUrl(user.ProfileImageObjectName);

                IList<string> roles = await _userManager.GetRolesAsync(user);
                primaryRole = roles.FirstOrDefault() ?? primaryRole;
            }
        }

        UserMenuViewModel model = new()
        {
            UserId = userId,
            DisplayName = displayName,
            Email = email,
            PrimaryRole = primaryRole,
            Initials = BuildInitials(displayName, email),
            ProfileImageUrl = profileImageUrl,
            ProfileUrl = $"/{culture}/profile",
            SettingsUrl = $"/{culture}/profile",
            LogoutUrl = $"/{culture}/auth/logout"
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

    private string? BuildProfileImageUrl(string? objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName) ||
            string.IsNullOrWhiteSpace(_storageOptions.Buckets.CongressImages))
        {
            return null;
        }

        string encodedBucketName = Uri.EscapeDataString(_storageOptions.Buckets.CongressImages.Trim());
        string encodedObjectName = string.Join(
            '/',
            objectName
                .Trim()
                .TrimStart('/')
                .Replace('\\', '/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Uri.EscapeDataString));

        return $"/public-assets/{encodedBucketName}/{encodedObjectName}";
    }

    private static string BuildDisplayName(AppUser user, string fallback)
    {
        string fullName = $"{user.Name} {user.Surname}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? fallback : fullName;
    }

    private static string BuildInitials(string displayName, string email)
    {
        string source = !string.IsNullOrWhiteSpace(displayName) ? displayName : email;

        string[] parts = source
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length >= 2)
            return string.Concat(parts[0][0], parts[^1][0]).ToUpperInvariant();

        if (parts.Length == 1 && parts[0].Length > 0)
            return parts[0][0].ToString().ToUpperInvariant();

        return "U";
    }
}
