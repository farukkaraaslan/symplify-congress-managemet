using Microsoft.AspNetCore.Mvc;
using Symplify.Portal.WebUI.Models.Pages;
using Symplify.Portal.WebUI.Services.PublicSite;

namespace Symplify.Portal.WebUI.Controllers;

public sealed class BoardsController : PortalControllerBase
{
    private readonly IPublicSiteService _publicSiteService;

    public BoardsController(
        IPublicSiteService publicSiteService,
        IPortalCultureService cultureService,
        ILogger<BoardsController> logger)
        : base(publicSiteService, cultureService, logger)
    {
        _publicSiteService = publicSiteService;
    }

    [HttpGet("boards")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            await LoadShellAsync("boards", string.Empty, cancellationToken);
            var data = await _publicSiteService.GetBoardsAsync(cancellationToken);
            SetLocalizedTitle("Portal.Boards.PageTitle");
            SetLocalizedSeoDescription("Portal.Boards.SeoDescription");
            return View(new BoardsIndexViewModel { Data = data });
        }
        catch (Exception exception) when (exception is PublicSiteApiException or HttpRequestException or TaskCanceledException)
        {
            return HandlePublicApiFailure(exception);
        }
    }
}
