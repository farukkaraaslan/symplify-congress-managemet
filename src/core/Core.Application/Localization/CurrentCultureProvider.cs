using System.Globalization;

namespace Core.Application.Localization;

public sealed class CurrentCultureProvider : ICurrentCultureProvider
{
    private const string DefaultCulture = "tr-TR";

    public CultureContext GetCurrent()
    {
        var requestedCulture = CultureInfo.CurrentUICulture?.Name;

        if (string.IsNullOrWhiteSpace(requestedCulture))
            requestedCulture = DefaultCulture;

        return new CultureContext(
            RequestedCulture: requestedCulture,
            BaseCulture: GetBaseCulture(requestedCulture));
    }

    private static string? GetBaseCulture(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
            return null;

        return culture
            .Split('-', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
    }
}
