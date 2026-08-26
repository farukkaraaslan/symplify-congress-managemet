using Microsoft.AspNetCore.Mvc;
using Symplify.Portal.WebUI.Models.Pages;
using Symplify.Portal.WebUI.Services.PublicSite;

namespace Symplify.Portal.WebUI.Controllers;

public sealed class DocumentsController : PortalControllerBase
{
    private readonly IPublicSiteService _publicSiteService;

    public DocumentsController(
        IPublicSiteService publicSiteService,
        IPortalCultureService cultureService,
        ILogger<DocumentsController> logger)
        : base(publicSiteService, cultureService, logger)
    {
        _publicSiteService = publicSiteService;
    }

    [HttpGet("documents")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            await LoadShellAsync("documents", string.Empty, cancellationToken);
            var data = await _publicSiteService.GetDocumentsAsync(cancellationToken);
            string? selectedType = Request.Query["type"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(selectedType))
            {
                ViewData["Title"] = selectedType.Trim();
                ViewData["SeoDescription"] = selectedType.Trim();
            }
            else
            {
                SetLocalizedTitle("Portal.Documents.PageTitle");
                SetLocalizedSeoDescription("Portal.Documents.SeoDescription");
            }

            return View(new DocumentsIndexViewModel { Data = data });
        }
        catch (Exception exception) when (exception is PublicSiteApiException or HttpRequestException or TaskCanceledException)
        {
            return HandlePublicApiFailure(exception);
        }
    }
}
