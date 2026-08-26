using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Features.AbstractBook.Models;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Persistence.Contexts;
using SubmissionAuthor = Symplify.BackOffice.Domain.Submission.Author;

namespace Symplify.BackOffice.Persistence.Repositories;

public sealed class AbstractBookRepository : IAbstractBookRepository
{
    private static readonly Regex BreakRegex = new(
        "<(br\\s*/?|/p|/div|/li|/h[1-6])>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TagRegex = new(
        "<[^>]+>",
        RegexOptions.Compiled);

    private static readonly Regex MultiSpaceRegex = new(
        "[ \\t]{2,}",
        RegexOptions.Compiled);

    private static readonly Regex MultiLineRegex = new(
        "\\n{3,}",
        RegexOptions.Compiled);

    private readonly BackOfficeDbContext _context;

    public AbstractBookRepository(BackOfficeDbContext context)
    {
        _context = context;
    }

    public async Task<string?> GetCongressLogoUrlAsync(
        Guid congressId,
        string? culture,
        CancellationToken cancellationToken)
    {
        string normalizedCulture = NormalizeCulture(culture);
        Guid? requestedLanguageId = await _context.Languages
            .AsNoTracking()
            .Where(x => x.DeletedDate == null && x.IsActive && x.Culture == normalizedCulture)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        Guid? defaultLanguageId = await _context.Languages
            .AsNoTracking()
            .Where(x => x.DeletedDate == null && x.IsActive && x.IsDefault)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        Congress? congress = await _context.Congresses
            .AsNoTracking()
            .Include(x => x.Organization)
            .FirstOrDefaultAsync(
                x => x.Id == congressId
                     && x.DeletedDate == null
                     && x.Status == CongressStatus.Published,
                cancellationToken);

        if (congress is null)
            return null;

        // Congress entity stores logo object names in LogoLightPath / LogoDarkPath.
        // When a congress-specific logo is not available, preserve the established
        // organization-logo fallback used by the congress management screens.
        string logo = FirstNonEmpty(
            congress.LogoLightPath,
            congress.LogoDarkPath,
            congress.Organization?.LogoLightPath,
            congress.Organization?.LogoDarkPath);

        return string.IsNullOrWhiteSpace(logo) ? null : logo;
    }

    public async Task<AbstractBookDocumentSourceDto?> GetDocumentSourceAsync(
        Guid congressId,
        IReadOnlyCollection<Guid> submissionIds,
        string? culture,
        CancellationToken cancellationToken)
    {
        string normalizedCulture = NormalizeCulture(culture);
        Guid? requestedLanguageId = await _context.Languages
            .AsNoTracking()
            .Where(x => x.DeletedDate == null && x.IsActive && x.Culture == normalizedCulture)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        Guid? defaultLanguageId = await _context.Languages
            .AsNoTracking()
            .Where(x => x.DeletedDate == null && x.IsActive && x.IsDefault)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        Guid? englishLanguageId = await _context.Languages
            .AsNoTracking()
            .Where(x => x.DeletedDate == null
                        && x.IsActive
                        && (x.Culture == "en-US" || x.Culture == "en"))
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        Congress? congress = await _context.Congresses
            .AsNoTracking()
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == congressId
                                      && x.DeletedDate == null
                                      && x.Status == CongressStatus.Published,
                cancellationToken);

        if (congress is null)
            return null;

        Guid[] normalizedSubmissionIds = submissionIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        if (normalizedSubmissionIds.Length == 0)
        {
            return BuildDocumentSource(
                congress,
                requestedLanguageId,
                defaultLanguageId,
                englishLanguageId,
                Array.Empty<AbstractBookSubmissionContentDto>());
        }

        var submissions = await _context.Submissions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Authors)
                .ThenInclude(x => x.Title)
                    .ThenInclude(x => x!.Translations)
            .Where(x => x.DeletedDate == null
                        && x.CongressId == congressId
                        && normalizedSubmissionIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        List<AbstractBookSubmissionContentDto> contents = submissions
            .Select(submission => new AbstractBookSubmissionContentDto
            {
                Id = submission.Id,
                SubmissionNumber = submission.SubmissionNumber?.Trim() ?? string.Empty,
                TurkishTitle = CleanInline(ReadSubmissionValue(
                    submission,
                    "Title",
                    "TurkishTitle",
                    "TitleTr",
                    "TitleTR")),
                EnglishTitle = CleanInline(ReadSubmissionValue(
                    submission,
                    "EnglishTitle",
                    "TitleEnglish",
                    "TitleEn",
                    "TitleEN")),
                TurkishAbstract = CleanBlock(ReadSubmissionValue(
                    submission,
                    "Abstract",
                    "TurkishAbstract",
                    "AbstractTr",
                    "AbstractTR",
                    "Summary",
                    "SummaryTr")),
                EnglishAbstract = CleanBlock(ReadSubmissionValue(
                    submission,
                    "EnglishAbstract",
                    "AbstractEnglish",
                    "AbstractEn",
                    "AbstractEN",
                    "SummaryEn")),
                TurkishKeywords = CleanInline(ReadSubmissionValue(
                    submission,
                    "Keywords",
                    "TurkishKeywords",
                    "KeywordsTr",
                    "KeywordsTR")),
                EnglishKeywords = CleanInline(ReadSubmissionValue(
                    submission,
                    "EnglishKeywords",
                    "KeywordsEnglish",
                    "KeywordsEn",
                    "KeywordsEN")),
                Authors = submission.Authors
                    .OrderByDescending(x => x.IsCorrespondingAuthor)
                    .ThenBy(GetAuthorTitleOrder)
                    .ThenBy(x => x.FirstName ?? string.Empty)
                    .ThenBy(x => x.LastName ?? string.Empty)
                    .Select(author => new AbstractBookAuthorDto(
                        author.Id,
                        BuildAuthorDisplayName(author, requestedLanguageId, defaultLanguageId),
                        BuildAuthorPlainName(author),
                        author.Institution?.Trim() ?? string.Empty,
                        NormalizeOrcid(author.Orcid),
                        author.Email?.Trim(),
                        author.IsCorrespondingAuthor,
                        author.Title?.Order > 0 ? author.Title.Order : int.MaxValue))
                    .ToList()
            })
            .ToList();

        return BuildDocumentSource(
            congress,
            requestedLanguageId,
            defaultLanguageId,
            englishLanguageId,
            contents);
    }

    private static int GetAuthorTitleOrder(SubmissionAuthor author)
    {
        if (author.Title is null || author.Title.Order <= 0)
            return int.MaxValue;

        return author.Title.Order;
    }

    private static AbstractBookDocumentSourceDto BuildDocumentSource(
        Congress congress,
        Guid? requestedLanguageId,
        Guid? defaultLanguageId,
        Guid? englishLanguageId,
        IReadOnlyList<AbstractBookSubmissionContentDto> submissions)
    {
        CongressTranslation? translation = congress.Translations
            .FirstOrDefault(x => requestedLanguageId.HasValue && x.LanguageId == requestedLanguageId.Value)
            ?? congress.Translations.FirstOrDefault(x => defaultLanguageId.HasValue && x.LanguageId == defaultLanguageId.Value)
            ?? congress.Translations.FirstOrDefault();

        CongressTranslation? englishTranslation = congress.Translations
            .FirstOrDefault(x => englishLanguageId.HasValue && x.LanguageId == englishLanguageId.Value);

        string city = FirstNonEmpty(
            ReadString(congress, "CityName"),
            ResolveNestedDisplay(congress, "City", requestedLanguageId, defaultLanguageId),
            ReadString(congress, "LocationCity"));

        string venue = FirstNonEmpty(
            ReadString(congress, "Venue"),
            ReadString(congress, "VenueName"),
            ReadString(congress, "Location"));

        return new AbstractBookDocumentSourceDto
        {
            CongressId = congress.Id,
            CongressCode = FirstNonEmpty(ReadString(congress, "Code"), ReadString(congress, "ShortName")),
            CongressName = FirstNonEmpty(translation?.Title, congress.Name),
            CongressEnglishName = FirstNonEmpty(englishTranslation?.Title, translation?.Title, congress.Name),
            CongressSubtitle = translation?.Subtitle?.Trim() ?? string.Empty,
            StartDate = congress.StartDate,
            EndDate = congress.EndDate,
            Venue = venue,
            City = city,
            Submissions = submissions
        };
    }

    private static string ReadSubmissionValue(object submission, params string[] aliases)
    {
        string direct = ReadFirstNonEmptyString(submission, aliases);
        if (!string.IsNullOrWhiteSpace(direct))
            return direct;

        HashSet<string> normalizedAliases = aliases
            .Select(NormalizeKey)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);

        foreach (PropertyInfo property in submission.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.PropertyType != typeof(string)
                || !property.Name.Contains("Json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string? json = property.GetValue(submission) as string;
            if (string.IsNullOrWhiteSpace(json))
                continue;

            string fromJson = ReadJsonValue(json, normalizedAliases);
            if (!string.IsNullOrWhiteSpace(fromJson))
                return fromJson;
        }

        return string.Empty;
    }

    private static string ReadJsonValue(string json, IReadOnlySet<string> aliases)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            return FindJsonValue(document.RootElement, aliases);
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }

    private static string FindJsonValue(JsonElement element, IReadOnlySet<string> aliases)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (aliases.Contains(NormalizeKey(property.Name)))
                {
                    string candidate = JsonElementToString(property.Value);
                    if (!string.IsNullOrWhiteSpace(candidate))
                        return candidate;
                }
            }

            foreach (JsonProperty property in element.EnumerateObject())
            {
                string nested = FindJsonValue(property.Value, aliases);
                if (!string.IsNullOrWhiteSpace(nested))
                    return nested;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in element.EnumerateArray())
            {
                string nested = FindJsonValue(item, aliases);
                if (!string.IsNullOrWhiteSpace(nested))
                    return nested;
            }
        }

        return string.Empty;
    }

    private static string JsonElementToString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Array => string.Join(", ", element.EnumerateArray()
                .Select(JsonElementToString)
                .Where(x => !string.IsNullOrWhiteSpace(x))),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => element.ToString(),
            _ => string.Empty
        };
    }

    private static string ResolveNestedDisplay(
        object source,
        string propertyName,
        Guid? requestedLanguageId,
        Guid? defaultLanguageId)
    {
        object? nested = source.GetType().GetProperty(propertyName)?.GetValue(source);
        if (nested is null)
            return string.Empty;

        object? translationsObject = nested.GetType().GetProperty("Translations")?.GetValue(nested);
        if (translationsObject is System.Collections.IEnumerable translations)
        {
            List<object> list = translations.Cast<object>().ToList();
            object? selected = list.FirstOrDefault(x => requestedLanguageId.HasValue
                                                        && ReadGuid(x, "LanguageId") == requestedLanguageId.Value)
                               ?? list.FirstOrDefault(x => defaultLanguageId.HasValue
                                                          && ReadGuid(x, "LanguageId") == defaultLanguageId.Value)
                               ?? list.FirstOrDefault();
            string translated = ReadFirstNonEmptyString(selected, "Name", "Title", "Description");
            if (!string.IsNullOrWhiteSpace(translated))
                return translated;
        }

        return ReadFirstNonEmptyString(nested, "Name", "Title", "Code");
    }

    private static Guid? ReadGuid(object? source, string propertyName)
    {
        object? value = source?.GetType().GetProperty(propertyName)?.GetValue(source);
        return value is Guid guid ? guid : null;
    }

    private static string BuildAuthorDisplayName(
        SubmissionAuthor author,
        Guid? requestedLanguageId,
        Guid? defaultLanguageId)
    {
        string title = ResolveAuthorTitle(author, requestedLanguageId, defaultLanguageId);
        string fullName = $"{author.FirstName} {author.LastName}".Trim();
        return string.IsNullOrWhiteSpace(title)
            ? fullName
            : $"{title} {fullName}".Trim();
    }

    private static string BuildAuthorPlainName(SubmissionAuthor author)
        => $"{author.FirstName} {author.LastName}".Trim();

    private static string ResolveAuthorTitle(
        SubmissionAuthor author,
        Guid? requestedLanguageId,
        Guid? defaultLanguageId)
    {
        if (author.Title is null)
            return string.Empty;

        var translation = author.Title.Translations
            .FirstOrDefault(x => requestedLanguageId.HasValue && x.LanguageId == requestedLanguageId.Value)
            ?? author.Title.Translations.FirstOrDefault(x => defaultLanguageId.HasValue && x.LanguageId == defaultLanguageId.Value)
            ?? author.Title.Translations.FirstOrDefault();

        return FirstNonEmpty(
            translation?.Description,
            translation?.Name,
            author.Title.Code);
    }

    private static string NormalizeOrcid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string normalized = value.Trim()
            .Replace("https://orcid.org/", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("http://orcid.org/", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("ORCID:", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        return normalized;
    }

    private static string CleanInline(string? value)
        => Clean(value).Replace("\n", " ", StringComparison.Ordinal).Trim();

    private static string CleanBlock(string? value)
        => Clean(value).Trim();

    private static string Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string text = BreakRegex.Replace(value, "\n");
        text = TagRegex.Replace(text, string.Empty);
        text = WebUtility.HtmlDecode(text);
        text = text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Replace('\u00A0', ' ');
        text = string.Join("\n", text.Split('\n').Select(x => MultiSpaceRegex.Replace(x.Trim(), " ")));
        text = MultiLineRegex.Replace(text, "\n\n");
        return text.Trim();
    }

    private static string ReadString(object? source, string propertyName)
        => source?.GetType().GetProperty(propertyName)?.GetValue(source) as string ?? string.Empty;

    private static string ReadFirstNonEmptyString(object? source, params string[] propertyNames)
    {
        if (source is null)
            return string.Empty;

        foreach (string propertyName in propertyNames)
        {
            string value = ReadString(source, propertyName);
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return string.Empty;
    }

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim() ?? string.Empty;

    private static string NormalizeKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        StringBuilder builder = new();
        foreach (char character in value.Trim().ToUpperInvariant())
        {
            if (char.IsLetterOrDigit(character))
                builder.Append(character);
        }

        return builder.ToString();
    }

    private static string NormalizeCulture(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
            return "tr-TR";
        if (string.Equals(culture, "tr", StringComparison.OrdinalIgnoreCase))
            return "tr-TR";
        if (string.Equals(culture, "en", StringComparison.OrdinalIgnoreCase))
            return "en-US";
        return culture;
    }
}
