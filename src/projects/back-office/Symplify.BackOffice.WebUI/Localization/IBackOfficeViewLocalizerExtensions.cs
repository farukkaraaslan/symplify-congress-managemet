using Microsoft.AspNetCore.Mvc.Localization;

namespace Symplify.BackOffice.WebUI.Localization;

public static class IBackOfficeViewLocalizerExtensions
{
    public static string GetStringValueSafe(this IBackOfficeViewLocalizer localizer, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        LocalizedHtmlString localized = localizer[key];

        return string.IsNullOrWhiteSpace(localized.Value)
            ? key
            : localized.Value;
    }
}
