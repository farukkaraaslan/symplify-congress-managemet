namespace Symplify.BackOffice.Application.Features.ParticipationCertificates.Services;

public static class ParticipationCertificateCultures
{
    public const string Turkish = "tr-TR";
    public const string English = "en-US";

    public static IReadOnlyList<string> Supported { get; } = new[]
    {
        Turkish,
        English
    };

    public static bool IsSupported(string? culture)
        => string.Equals(Normalize(culture), Turkish, StringComparison.OrdinalIgnoreCase) ||
           string.Equals(Normalize(culture), English, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
            return Turkish;

        string value = culture.Trim();

        if (value.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            return English;

        if (value.StartsWith("tr", StringComparison.OrdinalIgnoreCase))
            return Turkish;

        return value;
    }

    public static string GetDisplayName(string? culture)
        => string.Equals(Normalize(culture), English, StringComparison.OrdinalIgnoreCase)
            ? "English"
            : "Türkçe";

    public static string GetShortCode(string? culture)
        => string.Equals(Normalize(culture), English, StringComparison.OrdinalIgnoreCase)
            ? "en"
            : "tr";
}
