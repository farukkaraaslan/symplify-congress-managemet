namespace Symplify.BackOffice.WebUI.Localization;

public interface IBackOfficeResourceProvider
{
    string ResolveCurrentCulture();

    string NormalizeCulture(string? culture);

    string GetStringValue(string key, string? culture = null);

    IReadOnlyDictionary<string, string> GetResourcesByPrefixes(
        IEnumerable<string> prefixes,
        string? culture = null);
}
