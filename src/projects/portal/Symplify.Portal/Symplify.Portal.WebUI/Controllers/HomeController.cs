using Microsoft.AspNetCore.Mvc;
using Symplify.Portal.WebUI.Models;
using Symplify.Portal.WebUI.Models.Pages;
using Symplify.Portal.WebUI.Services.PublicSite;
using System.Diagnostics;

namespace Symplify.Portal.WebUI.Controllers;

public sealed class HomeController : PortalControllerBase
{
    private readonly IPublicSiteService _publicSiteService;

    public HomeController(
        IPublicSiteService publicSiteService,
        IPortalCultureService cultureService,
        ILogger<HomeController> logger)
        : base(publicSiteService, cultureService, logger)
    {
        _publicSiteService = publicSiteService;
    }

    [HttpGet("/")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            await LoadShellAsync("home", "home-index", cancellationToken);
            var home = await _publicSiteService.GetHomeAsync(cancellationToken);

            ViewData["SeoTitle"] = home.Organization.ShortName
                ?? home.Organization.Code
                ?? home.Organization.Name
                ?? home.Congress.Name
                ?? home.Congress.Title;
            ViewData["SeoDescription"] = home.Congress.SeoDescription
                ?? home.Congress.ShortDescription
                ?? home.Congress.Subtitle
                ?? home.Congress.WelcomeContent;

            return View(new HomeIndexViewModel { Home = home });
        }
        catch (Exception exception) when (exception is PublicSiteApiException or HttpRequestException or TaskCanceledException)
        {
            return HandlePublicApiFailure(exception);
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
