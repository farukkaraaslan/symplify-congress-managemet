using Microsoft.AspNetCore.Mvc;
using Symplify.Portal.WebUI.Models.PublicSite;
using Symplify.Portal.WebUI.Services.PublicSite;

namespace Symplify.Portal.WebUI.Controllers;

public abstract class PortalControllerBase : Controller
{
    private readonly IPublicSiteService _publicSiteService;
    private readonly IPortalCultureService _cultureService;
    private readonly ILogger _logger;

    protected PortalControllerBase(
        IPublicSiteService publicSiteService,
        IPortalCultureService cultureService,
        ILogger logger)
    {
        _publicSiteService = publicSiteService;
        _cultureService = cultureService;
        _logger = logger;
    }

    protected string CurrentCulture => _cultureService.GetCurrentCulture();

    protected async Task LoadShellAsync(string activeMenu, string bodyClass, CancellationToken cancellationToken)
    {
        Task shellTask = LoadShellCoreAsync(cancellationToken);
        Task documentTypesTask = LoadDocumentTypesForNavigationAsync(cancellationToken);

        await shellTask;
        await documentTypesTask;

        ViewData["ActiveMenu"] = activeMenu;
        ViewData["BodyClass"] = bodyClass;
        ViewData["CurrentCulture"] = CurrentCulture;
        Response.Cookies.Append(
            "Symplify.Portal.Culture",
            CurrentCulture,
            new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });
    }

    private async Task LoadShellCoreAsync(CancellationToken cancellationToken)
    {
        ViewData["Shell"] = await _publicSiteService.GetShellAsync(cancellationToken);
    }

    private async Task LoadDocumentTypesForNavigationAsync(CancellationToken cancellationToken)
    {
        try
        {
            ViewData["DocumentTypeNames"] = await _publicSiteService.GetDocumentTypeNamesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is PublicSiteApiException or HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(exception, "Document type navigation could not be loaded from Public API.");
            ViewData["DocumentTypeNames"] = Array.Empty<string>();
        }
    }


    protected void SetLocalizedTitle(string resourceKey)
    {
        ViewData["Title"] = GetLocalizedText(resourceKey);
    }

    protected void SetLocalizedSeoDescription(string resourceKey)
    {
        ViewData["SeoDescription"] = GetLocalizedText(resourceKey);
    }

    protected string GetLocalizedText(string resourceKey)
    {
        if (string.IsNullOrWhiteSpace(resourceKey))
            return string.Empty;

        if (ViewData["Shell"] is PublicSiteBootstrapResponse shell &&
            shell.Resources.TryGetValue(resourceKey, out string? value) &&
            !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return resourceKey;
    }

    protected IActionResult HandlePublicApiFailure(Exception exception, string activeMenu = "home", string bodyClass = "service-unavailable-page")
    {
        string currentCulture = CurrentCulture;

        _logger.LogError(exception, "Public site page could not be rendered because Public API request failed.");

        ViewData["Shell"] = new PublicSiteBootstrapResponse();
        ViewData["DocumentTypeNames"] = Array.Empty<string>();
        ViewData["ActiveMenu"] = activeMenu;
        ViewData["BodyClass"] = bodyClass;
        ViewData["CurrentCulture"] = currentCulture;

        Response.Cookies.Append(
            "Symplify.Portal.Culture",
            currentCulture,
            new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });

        return View("~/Views/Shared/PublicApiUnavailable.cshtml");
    }
}
