using Microsoft.AspNetCore.Mvc;
using Symplify.Portal.WebUI.Models.Pages;
using Symplify.Portal.WebUI.Services.PublicSite;

namespace Symplify.Portal.WebUI.Controllers;

public sealed class ContentsController : PortalControllerBase
{
    private readonly IPublicSiteService _publicSiteService;

    public ContentsController(
        IPublicSiteService publicSiteService,
        IPortalCultureService cultureService,
        ILogger<ContentsController> logger)
        : base(publicSiteService, cultureService, logger)
    {
        _publicSiteService = publicSiteService;
    }

    [HttpGet("contents")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            await LoadShellAsync("contents", string.Empty, cancellationToken);
            var data = await _publicSiteService.GetContentsAsync(cancellationToken);
            SetLocalizedTitle("Portal.PaymentPlans.PageTitle");
            SetLocalizedSeoDescription("Portal.PaymentPlans.SeoDescription");
            return View(new ContentsIndexViewModel { Data = data });
        }
        catch (Exception exception) when (exception is PublicSiteApiException or HttpRequestException or TaskCanceledException)
        {
            return HandlePublicApiFailure(exception);
        }
    }
}
