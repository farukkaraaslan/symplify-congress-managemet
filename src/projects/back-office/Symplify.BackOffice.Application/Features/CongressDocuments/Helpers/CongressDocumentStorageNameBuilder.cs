using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressDocuments.Helpers;

public static partial class CongressDocumentStorageNameBuilder
{
    public static string BuildObjectName(
        Congress congress,
        Guid documentId,
        string generatedFileName)
    {
        return string.Join(
            '/',
            new[]
            {
                "backoffice",
                "organizations",
                congress.OrganizationId.ToString("N"),
                "congresses",
                congress.Id.ToString("N"),
                "documents",
                documentId.ToString("N"),
                generatedFileName
            });
    }

    public static string BuildFileName(
        Congress congress,
        string? documentTypeName,
        Guid documentId,
        string originalFileName)
    {
        string extension = Path.GetExtension(originalFileName);

        if (string.IsNullOrWhiteSpace(extension))
            extension = ".bin";

        string congressCode = ToSlug(
            !string.IsNullOrWhiteSpace(congress.Code)
                ? congress.Code
                : congress.Name,
            "congress");

        string documentTypePart = ToSlug(
            documentTypeName,
            "document");

        if (congressCode.Length > 80)
            congressCode = congressCode[..80].Trim('-');

        if (documentTypePart.Length > 60)
            documentTypePart = documentTypePart[..60].Trim('-');

        string shortDocumentId = documentId.ToString("N")[..8];

        return $"{congressCode}-{documentTypePart}-{shortDocumentId}{extension.ToLowerInvariant()}";
    }

    private static string ToSlug(
        string? value,
        string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        string normalizedValue = ReplaceTurkishCharacters(value.Trim());
        normalizedValue = RemoveDiacritics(normalizedValue).ToLowerInvariant();

        string slug = InvalidCharactersRegex().Replace(normalizedValue, "-");
        slug = MultipleDashRegex().Replace(slug, "-");
        slug = slug.Trim('-', '.', ' ');

        return string.IsNullOrWhiteSpace(slug)
            ? fallback
            : slug;
    }

    private static string ReplaceTurkishCharacters(string value)
    {
        return value
            .Replace('Ç', 'C')
            .Replace('ç', 'c')
            .Replace('Ğ', 'G')
            .Replace('ğ', 'g')
            .Replace('İ', 'I')
            .Replace('ı', 'i')
            .Replace('Ö', 'O')
            .Replace('ö', 'o')
            .Replace('Ş', 'S')
            .Replace('ş', 's')
            .Replace('Ü', 'U')
            .Replace('ü', 'u');
    }

    private static string RemoveDiacritics(string value)
    {
        string normalized = value.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new();

        foreach (char character in normalized)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);

            if (category != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex("[^a-z0-9._-]+", RegexOptions.Compiled)]
    private static partial Regex InvalidCharactersRegex();

    [GeneratedRegex("-{2,}", RegexOptions.Compiled)]
    private static partial Regex MultipleDashRegex();
}
