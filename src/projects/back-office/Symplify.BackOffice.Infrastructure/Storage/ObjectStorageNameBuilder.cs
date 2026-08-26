using System.Text;
using System.Text.RegularExpressions;

namespace Symplify.BackOffice.Infrastructure.Storage;

public static partial class ObjectStorageNameBuilder
{
    public static string Build(
        params string?[] segments)
    {
        IEnumerable<string> sanitizedSegments = segments
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .Select(segment => SanitizeSegment(segment!))
            .Where(segment => !string.IsNullOrWhiteSpace(segment));

        return string.Join('/', sanitizedSegments);
    }

    public static string BuildFileName(
        string? originalFileName,
        string? fallbackExtension = null)
    {
        string extension = Path.GetExtension(originalFileName ?? string.Empty);

        if (string.IsNullOrWhiteSpace(extension) && !string.IsNullOrWhiteSpace(fallbackExtension))
        {
            extension = fallbackExtension.StartsWith('.')
                ? fallbackExtension
                : $".{fallbackExtension}";
        }

        return $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
    }

    private static string SanitizeSegment(string value)
    {
        string normalized = value.Trim().Replace('\\', '/');
        string[] parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return string.Join('/', parts.Select(SanitizeSingleSegment));
    }

    private static string SanitizeSingleSegment(string value)
    {
        string ascii = RemoveDiacritics(value).ToLowerInvariant();
        string sanitized = InvalidCharactersRegex().Replace(ascii, "-");
        sanitized = MultipleDashRegex().Replace(sanitized, "-");

        return sanitized.Trim('-', '.', ' ');
    }

    private static string RemoveDiacritics(string value)
    {
        string normalized = value.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new();

        foreach (char character in normalized)
        {
            System.Globalization.UnicodeCategory category =
                System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character);

            if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex("[^a-z0-9._-]+", RegexOptions.Compiled)]
    private static partial Regex InvalidCharactersRegex();

    [GeneratedRegex("-{2,}", RegexOptions.Compiled)]
    private static partial Regex MultipleDashRegex();
}
