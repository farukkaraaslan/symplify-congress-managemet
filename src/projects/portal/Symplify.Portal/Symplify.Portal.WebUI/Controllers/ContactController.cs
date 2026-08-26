using Microsoft.AspNetCore.Mvc;
using Symplify.Portal.WebUI.Models.Pages;
using Symplify.Portal.WebUI.Services.PublicSite;

namespace Symplify.Portal.WebUI.Controllers;

public sealed class ContactController : PortalControllerBase
{
    private readonly IPublicSiteService _publicSiteService;

    public ContactController(
        IPublicSiteService publicSiteService,
        IPortalCultureService cultureService,
        ILogger<ContactController> logger)
        : base(publicSiteService, cultureService, logger)
    {
        _publicSiteService = publicSiteService;
    }

    [HttpGet("contact")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            await LoadShellAsync("contact", string.Empty, cancellationToken);
            var data = await _publicSiteService.GetContactAsync(cancellationToken);
            SetLocalizedTitle("Portal.Contact.PageTitle");
            SetLocalizedSeoDescription("Portal.Contact.SeoDescription");
            return View(new ContactIndexViewModel { Data = data });
        }
        catch (Exception exception) when (exception is PublicSiteApiException or HttpRequestException or TaskCanceledException)
        {
            return HandlePublicApiFailure(exception);
        }
    }
}
