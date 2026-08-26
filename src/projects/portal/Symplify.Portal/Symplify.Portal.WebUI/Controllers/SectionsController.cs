using Microsoft.AspNetCore.Mvc;
using Symplify.Portal.WebUI.Helpers;
using Symplify.Portal.WebUI.Models.Pages;
using Symplify.Portal.WebUI.Services.PublicSite;

namespace Symplify.Portal.WebUI.Controllers;

public sealed class SectionsController : PortalControllerBase
{
    private readonly IPublicSiteService _publicSiteService;

    public SectionsController(
        IPublicSiteService publicSiteService,
        IPortalCultureService cultureService,
        ILogger<SectionsController> logger)
        : base(publicSiteService, cultureService, logger)
    {
        _publicSiteService = publicSiteService;
    }

    [HttpGet("sections")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            await LoadShellAsync("sections", string.Empty, cancellationToken);

            Task<Symplify.Portal.WebUI.Models.PublicSite.PublicSectionsResponse> sectionsTask =
                _publicSiteService.GetSectionsAsync(cancellationToken);
            Task<Symplify.Portal.WebUI.Models.PublicSite.PublicContentsResponse> contentsTask =
                _publicSiteService.GetContentsAsync(cancellationToken);

            await Task.WhenAll(sectionsTask, contentsTask);

            var sectionsData = await sectionsTask;
            var visibleSections = new Symplify.Portal.WebUI.Models.PublicSite.PublicSectionsResponse
            {
                Congress = sectionsData.Congress,
                Sections = sectionsData.Sections
                    .Where(section => !PortalSectionClassifier.IsPaymentSection(section))
                    .ToArray()
            };

            SetLocalizedTitle("Portal.Sections.PageTitle");
            SetLocalizedSeoDescription("Portal.Sections.SeoDescription");
            return View(new SectionsIndexViewModel
            {
                Data = visibleSections,
                Contents = await contentsTask
            });
        }
        catch (Exception exception) when (exception is PublicSiteApiException or HttpRequestException or TaskCanceledException)
        {
            return HandlePublicApiFailure(exception);
        }
    }

    [HttpGet("sections/{bindingKey}")]
    public async Task<IActionResult> Detail([FromRoute] string bindingKey, CancellationToken cancellationToken)
    {
        try
        {
            await LoadShellAsync("sections", string.Empty, cancellationToken);
            var section = await _publicSiteService.GetSectionByBindingKeyAsync(bindingKey, cancellationToken);

            if (section is null)
                return NotFound();

            if (PortalSectionClassifier.IsPaymentSection(section))
            {
                return Redirect($"/payment?culture={Uri.EscapeDataString(CurrentCulture)}");
            }

            ViewData["Title"] = section.Title;
            ViewData["SeoDescription"] = PortalSeoHelper.BuildDescriptionFromContent(
                section.Title,
                section.Content);

            return View(new SectionDetailViewModel { Section = section });
        }
        catch (Exception exception) when (exception is PublicSiteApiException or HttpRequestException or TaskCanceledException)
        {
            return HandlePublicApiFailure(exception);
        }
    }
}
