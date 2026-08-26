using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Symplify.BackOffice.Domain.Identity;

namespace Symplify.BackOffice.WebUI.Middleware;

public sealed class RequirePhoneNumberMiddleware
{
    private const string CompletePhonePathSuffix = "/profile/complete-phone";
    private readonly RequestDelegate _next;

    public RequirePhoneNumberMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, UserManager<AppUser> userManager)
    {
        if (ShouldBypass(context))
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            await _next(context);
            return;
        }

        AppUser? user = await userManager.FindByIdAsync(userId);
        if (user is null || !string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            await _next(context);
            return;
        }

        string culture = ResolveCulture(context.Request.Path);
        string returnUrl = BuildReturnUrl(context.Request);
        string completePhoneUrl = $"/{culture}/profile/complete-phone?returnUrl={Uri.EscapeDataString(returnUrl)}";

        context.Response.Redirect(completePhoneUrl);
    }

    private static bool ShouldBypass(HttpContext context)
    {
        PathString path = context.Request.Path;
        string value = path.Value ?? string.Empty;

        if (string.IsNullOrWhiteSpace(value))
            return true;

        if (HttpMethods.IsOptions(context.Request.Method))
            return true;

        if (value.Contains(CompletePhonePathSuffix, StringComparison.OrdinalIgnoreCase))
            return true;

        string normalized = value.ToLowerInvariant();

        return normalized.Contains("/auth/", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("/auth", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("/logout", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("/error", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("/public/certificates/", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("/js/", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("/_framework/", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveCulture(PathString path)
    {
        string value = path.Value ?? string.Empty;
        string[] parts = value.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length > 0 && IsCultureSegment(parts[0]))
            return parts[0];

        return "tr-TR";
    }

    private static bool IsCultureSegment(string value)
    {
        if (value.Length != 5 || value[2] != '-')
            return false;

        return char.IsLetter(value[0]) &&
               char.IsLetter(value[1]) &&
               char.IsLetter(value[3]) &&
               char.IsLetter(value[4]);
    }

    private static string BuildReturnUrl(HttpRequest request)
    {
        string path = request.Path.HasValue ? request.Path.Value! : "/";
        string query = request.QueryString.HasValue ? request.QueryString.Value! : string.Empty;
        return path + query;
    }
}
