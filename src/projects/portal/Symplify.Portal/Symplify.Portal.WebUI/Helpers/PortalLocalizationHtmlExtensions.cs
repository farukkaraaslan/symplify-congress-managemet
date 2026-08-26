using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Symplify.Portal.WebUI.Models.PublicSite;

namespace Symplify.Portal.WebUI.Helpers;

public static class PortalLocalizationHtmlExtensions
{
    public static string Text(this IHtmlHelper htmlHelper, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        if (htmlHelper.ViewData["Shell"] is PublicSiteBootstrapResponse shell &&
            shell.Resources.TryGetValue(key, out string? value) &&
            !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return key;
    }

    public static IHtmlContent HtmlText(this IHtmlHelper htmlHelper, string key)
    {
        return new HtmlString(System.Net.WebUtility.HtmlEncode(htmlHelper.Text(key)));
    }
}
