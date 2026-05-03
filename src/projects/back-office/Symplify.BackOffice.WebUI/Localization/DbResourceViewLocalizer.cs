using System.Globalization;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;

namespace Symplify.BackOffice.WebUI.Localization;

public sealed class DbResourceViewLocalizer : IBackOfficeViewLocalizer
{
    private readonly IBackOfficeResourceProvider _resourceProvider;

    public DbResourceViewLocalizer(IBackOfficeResourceProvider resourceProvider)
    {
        _resourceProvider = resourceProvider;
    }

    public LocalizedHtmlString this[string name]
    {
        get
        {
            string value = GetStringValue(name);

            return ToLocalizedHtmlString(name, value);
        }
    }

    public LocalizedHtmlString this[string name, params object[] arguments]
    {
        get
        {
            string value = GetStringValue(name);

            string formattedValue = arguments.Length == 0
                ? value
                : string.Format(CultureInfo.CurrentCulture, value, arguments);

            return ToLocalizedHtmlString(name, formattedValue);
        }
    }

    public LocalizedString GetString(string name)
    {
        string value = GetStringValue(name);

        return ToLocalizedString(name, value);
    }

    public LocalizedString GetString(string name, params object[] arguments)
    {
        string value = GetStringValue(name);

        string formattedValue = arguments.Length == 0
            ? value
            : string.Format(CultureInfo.CurrentCulture, value, arguments);

        return ToLocalizedString(name, formattedValue);
    }

    public string GetStringValue(string key)
    {
        return _resourceProvider.GetStringValue(key);
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        string culture = _resourceProvider.ResolveCurrentCulture();
        IReadOnlyDictionary<string, string> resources = _resourceProvider.GetResourcesByPrefixes(
            new[]
            {
                "Common.",
                "BackOffice."
            },
            culture);

        return resources.Select(item => new LocalizedString(
            item.Key,
            item.Value,
            resourceNotFound: false));
    }

    private static LocalizedHtmlString ToLocalizedHtmlString(string name, string value)
    {
        return new LocalizedHtmlString(
            name,
            value,
            isResourceNotFound: value == name);
    }

    private static LocalizedString ToLocalizedString(string name, string value)
    {
        return new LocalizedString(
            name,
            value,
            resourceNotFound: value == name);
    }
}
