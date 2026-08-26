using System.Globalization;
using System.Text.RegularExpressions;

namespace Symplify.BackOffice.Application.Common.Text;

public static partial class BackOfficeTextNormalizer
{
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo("en-US");

    private static readonly Dictionary<string, string> InstitutionExactReplacements = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Jsga"] = "JSGA",
        ["Meb"] = "MEB",
        ["Sbü"] = "SBÜ",
        ["Sbu"] = "SBÜ",
        ["Tc"] = "T.C.",
        ["T.c"] = "T.C.",
        ["Suam"] = "SUAM",
        ["Eah"] = "EAH",
        ["Myo"] = "MYO",
        ["Ktü"] = "KTÜ",
        ["Omu"] = "OMÜ",
        ["Kto"] = "KTO",
        ["Makü"] = "MAKÜ",
        ["Ybu"] = "YBU",
        ["Adpu"] = "ADPU",
        ["Uaem"] = "UAEM",
        ["Mcbu"] = "MCBU",
        ["Rudn"] = "RUDN",
        ["Toaurılc"] = "TOAURILC",
        ["Toaurilc"] = "TOAURILC",
        ["Ad"] = "AD",
        ["Abd"] = "ABD",
        ["S.y.k"] = "S.Y.K.",
        ["S.y.k."] = "S.Y.K.",
        ["R&d"] = "R&D",
        ["Ar-ge"] = "AR-GE",
        ["İnc"] = "Inc",
        ["İnstitute"] = "Institute",
        ["İndustry"] = "Industry",
        ["İnternational"] = "International",
        ["İraq"] = "Iraq",
        ["İtaly"] = "Italy"
    };

    public static string? NormalizePersonFirstName(string? value)
    {
        string? normalized = NormalizeWhiteSpace(value);
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        return CapitalizeByDelimiters(normalized, TurkishCulture);
    }

    public static string? NormalizePersonSurname(string? value)
    {
        string? normalized = NormalizeWhiteSpace(value);
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        return normalized.ToUpper(TurkishCulture);
    }

    public static string NormalizeRequiredPersonFirstName(string? value)
        => NormalizePersonFirstName(value) ?? string.Empty;

    public static string NormalizeRequiredPersonSurname(string? value)
        => NormalizePersonSurname(value) ?? string.Empty;

    public static string NormalizePersonFullName(string? firstName, string? lastName)
    {
        return string.Join(' ', new[]
            {
                NormalizePersonFirstName(firstName),
                NormalizePersonSurname(lastName)
            }
            .Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    public static (string FirstName, string LastName) NormalizeAuthorNameParts(
        string? firstName,
        string? lastName,
        string? fullName)
    {
        string? normalizedFirstName = NormalizePersonFirstName(firstName);
        string? normalizedLastName = NormalizePersonSurname(lastName);

        if (!string.IsNullOrWhiteSpace(normalizedFirstName) || !string.IsNullOrWhiteSpace(normalizedLastName))
            return (normalizedFirstName ?? string.Empty, normalizedLastName ?? string.Empty);

        return NormalizeFullNameByLastToken(fullName);
    }

    public static (string FirstName, string LastName) NormalizeFullNameByLastToken(string? fullName)
    {
        string? normalized = NormalizeWhiteSpace(fullName);
        if (string.IsNullOrWhiteSpace(normalized))
            return (string.Empty, string.Empty);

        string[] parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
            return (NormalizeRequiredPersonFirstName(parts[0]), string.Empty);

        string lastName = parts[^1];
        string firstName = string.Join(' ', parts.Take(parts.Length - 1));

        return (NormalizeRequiredPersonFirstName(firstName), NormalizeRequiredPersonSurname(lastName));
    }

    public static string? NormalizeSubmissionTitleTr(string? value)
    {
        string? normalized = NormalizeTitleText(value);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized.ToUpper(TurkishCulture);
    }

    public static string? NormalizeSubmissionTitleEn(string? value)
    {
        string? normalized = NormalizeTitleText(value);
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        normalized = ReplaceTurkishCharactersForEnglish(normalized);
        return normalized.ToUpper(EnglishCulture);
    }

    public static string NormalizeRequiredSubmissionTitleTr(string? value)
        => NormalizeSubmissionTitleTr(value) ?? string.Empty;

    public static string NormalizeRequiredSubmissionTitleEn(string? value)
        => NormalizeSubmissionTitleEn(value) ?? string.Empty;

    public static string? NormalizeEnglishText(string? value)
    {
        string? normalized = NormalizeWhiteSpace(value);
        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : ReplaceTurkishCharactersForEnglish(normalized);
    }

    public static string? NormalizeInstitution(string? value)
    {
        string? normalized = NormalizeInstitutionText(value);
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        string result = CapitalizeByDelimiters(normalized, TurkishCulture);

        result = WordBoundaryRegex().Replace(result, match =>
            InstitutionExactReplacements.TryGetValue(match.Value, out string? replacement)
                ? replacement
                : match.Value);

        result = Regex.Replace(result, "\\bVe\\b", "ve");
        result = Regex.Replace(result, "\\bİle\\b", "ile");
        result = Regex.Replace(result, "\\bAnd\\b", "and", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        result = Regex.Replace(result, "\\bOf\\b", "of", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        result = Regex.Replace(result, "\\bThe\\b", "the", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        result = Regex.Replace(result, "\\bFor\\b", "for", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        result = Regex.Replace(result, "\\bIn\\b", "in", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        result = Regex.Replace(result, @"S\.\s*B\.\s*Ü\.?", "SBÜ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        result = Regex.Replace(result, @"A\.\s*D\.?", "A.D.", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        result = Regex.Replace(result, @"A\.\s*B\.\s*D\.?", "A.B.D.", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        result = Regex.Replace(result, @"S\.\s*Y\.\s*K\.?", "S.Y.K.", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        result = Regex.Replace(result, @"Dr\.\s*Lütfi", "Dr. Lütfi", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        result = Regex.Replace(result, @"Dr\.\s*Sadi", "Dr. Sadi", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        return result.Trim();
    }

    public static string ReplaceTurkishCharactersForEnglish(string value)
    {
        return value
            .Replace('ç', 'c')
            .Replace('Ç', 'C')
            .Replace('ğ', 'g')
            .Replace('Ğ', 'G')
            .Replace('ı', 'i')
            .Replace('İ', 'I')
            .Replace('ö', 'o')
            .Replace('Ö', 'O')
            .Replace('ş', 's')
            .Replace('Ş', 'S')
            .Replace('ü', 'u')
            .Replace('Ü', 'U');
    }

    public static string? NormalizeWhiteSpace(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        string normalized = value.Trim().Replace('\t', ' ');
        normalized = MultiSpaceRegex().Replace(normalized, " ");
        return normalized.Length == 0 ? null : normalized;
    }

    public static string? NormalizeTitleText(string? value)
    {
        string? normalized = NormalizeWhiteSpace(value);
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        normalized = Regex.Replace(normalized, "\\s+([,.;:!?])", "$1");
        normalized = Regex.Replace(normalized, "([,;:!?])([^\\s])", "$1 $2");
        return normalized.Trim();
    }

    public static string? NormalizeInstitutionText(string? value)
    {
        string? normalized = NormalizeWhiteSpace(value);
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        normalized = Regex.Replace(normalized, "\\s+([,.;:])", "$1");
        normalized = Regex.Replace(normalized, "([,;:])([^\\s])", "$1 $2");
        normalized = Regex.Replace(normalized, "\\s*/\\s*", "/");
        normalized = Regex.Replace(normalized, "\\s*-\\s*", "-");
        normalized = Regex.Replace(normalized, "([\\p{L}])\\.([\\p{L}])", "$1. $2");

        return normalized.Trim();
    }

    private static string CapitalizeByDelimiters(string value, CultureInfo culture)
    {
        string lower = value.ToLower(culture);
        char[] chars = lower.ToCharArray();
        bool shouldUpper = true;

        for (int i = 0; i < chars.Length; i++)
        {
            char current = chars[i];

            if (char.IsLetter(current) && shouldUpper)
            {
                chars[i] = char.ToUpper(current, culture);
                shouldUpper = false;
                continue;
            }

            shouldUpper = current is ' ' or '-' or '\'' or '.' or '/' or ',' or ';' or ':' or '(' or '[' or '{';
        }

        return new string(chars).Trim();
    }

    [GeneratedRegex("\\s+")]
    private static partial Regex MultiSpaceRegex();

    [GeneratedRegex("[\\p{L}\\p{N}&.]+")]
    private static partial Regex WordBoundaryRegex();
}
