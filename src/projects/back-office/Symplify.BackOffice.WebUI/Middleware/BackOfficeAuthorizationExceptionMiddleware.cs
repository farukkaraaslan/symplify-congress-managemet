using System.Text.Encodings.Web;
using Core.Application.Pipelines.Authorization;
using Core.CrossCuttingConcerns.Exceptions.Types;

namespace Symplify.BackOffice.WebUI.Middleware;

public sealed class BackOfficeAuthorizationExceptionMiddleware
{
    private const string DefaultCulture = "tr-TR";

    private readonly RequestDelegate _next;
    private readonly ILogger<BackOfficeAuthorizationExceptionMiddleware> _logger;

    public BackOfficeAuthorizationExceptionMiddleware(
        RequestDelegate next,
        ILogger<BackOfficeAuthorizationExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AuthorizationException exception)
        {
            if (context.Response.HasStarted)
                throw;

            _logger.LogWarning(exception, "Application authorization failed. Path: {Path}", context.Request.Path);

            if (IsAjaxOrJsonRequest(context))
            {
                await WriteJsonAuthorizationResponseAsync(context);
                return;
            }

            if (context.User.Identity?.IsAuthenticated == true)
            {
                RedirectToAccessDenied(context);
                return;
            }

            RedirectToLogin(context);
        }
    }

    private static void RedirectToLogin(HttpContext context)
    {
        string culture = ResolveCulture(context);
        string returnUrl = GetReturnUrl(context);
        string loginUrl = $"/{culture}/auth/login?returnUrl={UrlEncoder.Default.Encode(returnUrl)}";
        context.Response.Redirect(loginUrl);
    }

    private static void RedirectToAccessDenied(HttpContext context)
    {
        string culture = ResolveCulture(context);
        context.Response.Redirect($"/{culture}/auth/access-denied");
    }

    private static string ResolveCulture(HttpContext context)
    {
        string? cultureFromItems = context.Items["CurrentCulture"]?.ToString();

        if (!string.IsNullOrWhiteSpace(cultureFromItems))
            return cultureFromItems;

        string? routeCulture = context.Request.RouteValues["culture"]?.ToString();

        if (!string.IsNullOrWhiteSpace(routeCulture))
            return routeCulture;

        string? firstPathSegment = context.Request.Path.Value?
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(firstPathSegment) && LooksLikeCulture(firstPathSegment))
            return firstPathSegment;

        return DefaultCulture;
    }

    private static string GetReturnUrl(HttpContext context)
    {
        string path = context.Request.Path.HasValue ? context.Request.Path.Value! : "/";
        string queryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value! : string.Empty;
        return $"{path}{queryString}";
    }

    private static bool LooksLikeCulture(string value)
    {
        return value.Length is >= 2 and <= 15 &&
               value.All(character => char.IsLetter(character) || character == '-' || character == '_');
    }

    private static bool IsAjaxOrJsonRequest(HttpContext context)
    {
        bool isAjax = string.Equals(
            context.Request.Headers.XRequestedWith,
            "XMLHttpRequest",
            StringComparison.OrdinalIgnoreCase);

        bool acceptsJson = context.Request.Headers.Accept
            .Any(value => value?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);

        return isAjax || acceptsJson;
    }

    private static async Task WriteJsonAuthorizationResponseAsync(HttpContext context)
    {
        bool isAuthenticated = context.User.Identity?.IsAuthenticated == true;

        context.Response.Clear();
        context.Response.StatusCode = isAuthenticated
            ? StatusCodes.Status403Forbidden
            : StatusCodes.Status401Unauthorized;

        context.Response.ContentType = "application/json";

        string message = isAuthenticated
            ? "You are not authorized to access this resource."
            : "Authentication is required.";

        await context.Response.WriteAsJsonAsync(new
        {
            statusCode = context.Response.StatusCode,
            message
        });
    }
}
