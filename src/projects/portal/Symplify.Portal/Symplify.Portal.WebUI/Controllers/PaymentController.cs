using Microsoft.AspNetCore.Mvc;
using Symplify.Portal.WebUI.Helpers;
using Symplify.Portal.WebUI.Models.Pages;
using Symplify.Portal.WebUI.Models.PublicSite;
using Symplify.Portal.WebUI.Services.PublicSite;

namespace Symplify.Portal.WebUI.Controllers;

public sealed class PaymentController : PortalControllerBase
{
    private const string PaymentResourceKey = "Portal.Navigation.Payment";

    private readonly IPublicSiteService _publicSiteService;

    public PaymentController(
        IPublicSiteService publicSiteService,
        IPortalCultureService cultureService,
        ILogger<PaymentController> logger)
        : base(publicSiteService, cultureService, logger)
    {
        _publicSiteService = publicSiteService;
    }

    [HttpGet("payment")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            await LoadShellAsync("payment", string.Empty, cancellationToken);

            PublicSectionsResponse sections = await _publicSiteService.GetSectionsAsync(cancellationToken);
            PublicSectionResponse? paymentSection = sections.Sections
                .OrderBy(section => section.Order)
                .FirstOrDefault(PortalSectionClassifier.IsPaymentSection);

            string pageTitle = GetPaymentPageTitle();
            ViewData["Title"] = pageTitle;
            ViewData["SeoDescription"] = PortalSeoHelper.BuildDescriptionFromContent(
                pageTitle,
                paymentSection?.Content);

            return View(new PaymentIndexViewModel
            {
                PageTitle = pageTitle,
                Section = paymentSection
            });
        }
        catch (Exception exception) when (exception is PublicSiteApiException or HttpRequestException or TaskCanceledException)
        {
            return HandlePublicApiFailure(exception, "payment");
        }
    }

    private string GetPaymentPageTitle()
    {
        string localizedTitle = GetLocalizedText(PaymentResourceKey);
        if (!string.Equals(localizedTitle, PaymentResourceKey, StringComparison.Ordinal))
        {
            return localizedTitle;
        }

        return CurrentCulture.StartsWith("en", StringComparison.OrdinalIgnoreCase)
            ? "Payment"
            : "Ödeme";
    }
}
