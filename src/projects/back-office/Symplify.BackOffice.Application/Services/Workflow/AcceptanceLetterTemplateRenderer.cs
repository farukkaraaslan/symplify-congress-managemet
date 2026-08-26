using System.Text.Json;
using System.Text.RegularExpressions;

namespace Symplify.BackOffice.Application.Services.Workflow;

public interface IAcceptanceLetterTemplateRenderer
{
    Task<RenderedAcceptanceLetterTemplate> RenderAsync(
        AcceptanceLetterTemplateRenderRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class FileAcceptanceLetterTemplateRenderer : IAcceptanceLetterTemplateRenderer
{
    private static readonly Regex TokenRegex = new(
        "\\{\\{\\s*(?<key>[A-Za-z0-9_.-]+)\\s*\\}\\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<RenderedAcceptanceLetterTemplate> RenderAsync(
        AcceptanceLetterTemplateRenderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        AcceptanceLetterPdfTemplate template = await LoadTemplateAsync(request.Culture, cancellationToken);
        IReadOnlyDictionary<string, string> values = request.Values ?? new Dictionary<string, string>();
        IReadOnlyList<string> bodyParagraphs = ResolveBodyParagraphs(template.BodyParagraphs);

        return new RenderedAcceptanceLetterTemplate
        {
            HeaderTitle = RenderValue(template.HeaderTitle, values),
            SubmissionCodeLabel = RenderValue(template.SubmissionCodeLabel, values),
            VerificationTitle = RenderValue(template.VerificationTitle, values),
            VerificationCodeLabel = RenderValue(template.VerificationCodeLabel, values),
            VerificationUrlLabel = RenderValue(template.VerificationUrlLabel, values),
            SignerFallbackDuty = RenderValue(template.SignerFallbackDuty, values),
            BodyParagraphs = bodyParagraphs
                .Select(item => RenderValue(item, values))
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToArray()
        };
    }

    private static async Task<AcceptanceLetterPdfTemplate> LoadTemplateAsync(
        string? culture,
        CancellationToken cancellationToken)
    {
        foreach (string candidate in BuildCandidatePaths(culture))
        {
            if (!File.Exists(candidate))
                continue;

            await using FileStream stream = File.OpenRead(candidate);

            AcceptanceLetterPdfTemplate? template = await JsonSerializer.DeserializeAsync<AcceptanceLetterPdfTemplate>(
                stream,
                JsonOptions,
                cancellationToken);

            if (template is not null)
                return template;
        }

        return AcceptanceLetterPdfTemplate.Default;
    }

    private static IEnumerable<string> BuildCandidatePaths(string? culture)
    {
        string normalizedCulture = string.IsNullOrWhiteSpace(culture)
            ? "en"
            : culture.Trim();

        string[] templateRoots =
        {
            Path.Combine(AppContext.BaseDirectory, "Templates", "AcceptanceLetters"),
            Path.Combine(AppContext.BaseDirectory, "Services", "Workflow", "Templates", "AcceptanceLetters"),
            Path.Combine(Directory.GetCurrentDirectory(), "Templates", "AcceptanceLetters"),
            Path.Combine(Directory.GetCurrentDirectory(), "Services", "Workflow", "Templates", "AcceptanceLetters")
        };

        foreach (string templateRoot in templateRoots.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            yield return Path.Combine(templateRoot, $"acceptance-letter.{normalizedCulture}.json");

            int dashIndex = normalizedCulture.IndexOf('-', StringComparison.Ordinal);
            if (dashIndex > 0)
                yield return Path.Combine(templateRoot, $"acceptance-letter.{normalizedCulture[..dashIndex]}.json");

            yield return Path.Combine(templateRoot, "acceptance-letter.en.json");
        }
    }

    private static IReadOnlyList<string> ResolveBodyParagraphs(IReadOnlyList<string>? bodyParagraphs)
    {
        return bodyParagraphs is { Count: > 0 }
            ? bodyParagraphs
            : AcceptanceLetterPdfTemplate.DefaultBodyParagraphs;
    }

    private static string RenderValue(string? templateValue, IReadOnlyDictionary<string, string> values)
    {
        if (string.IsNullOrWhiteSpace(templateValue))
            return string.Empty;

        return TokenRegex.Replace(templateValue, match =>
        {
            string key = match.Groups["key"].Value;
            return values.TryGetValue(key, out string? value)
                ? value
                : string.Empty;
        });
    }
}

public sealed class AcceptanceLetterTemplateRenderRequest
{
    public string Culture { get; init; } = "en-US";

    public IReadOnlyDictionary<string, string> Values { get; init; } =
        new Dictionary<string, string>();
}

public sealed class RenderedAcceptanceLetterTemplate
{
    public string HeaderTitle { get; init; } = "ACCEPTANCE LETTER";

    public string SubmissionCodeLabel { get; init; } = "Submission Code";

    public string VerificationTitle { get; init; } = "Document Verification";

    public string VerificationCodeLabel { get; init; } = "Verification Code";

    public string VerificationUrlLabel { get; init; } = "Verify";

    public string SignerFallbackDuty { get; init; } = "Chairman of the Organizing Committee";

    public IReadOnlyList<string> BodyParagraphs { get; init; } = Array.Empty<string>();
}

public sealed class AcceptanceLetterPdfTemplate
{
    public static readonly IReadOnlyList<string> DefaultBodyParagraphs = new[]
    {
        "Dear {{AuthorFullName}},",
        "Your application for {{SubmissionTypeName}} with the theme \"{{SubmissionTitle}}\" to be presented at the {{CongressTitle}} to be held between {{CongressDateRange}} was accepted after the review and editorial approval process. Preparation guidelines are available through the official {{OrganizationShortName}} announcements.",
        "Thank you for your interest and we wish you continued success in your academic work."
    };

    public static AcceptanceLetterPdfTemplate Default { get; } = new()
    {
        HeaderTitle = "ACCEPTANCE LETTER",
        SubmissionCodeLabel = "Submission Code",
        VerificationTitle = "Document Verification",
        VerificationCodeLabel = "Verification Code",
        VerificationUrlLabel = "Verify",
        SignerFallbackDuty = "Chairman of the Organizing Committee",
        BodyParagraphs = DefaultBodyParagraphs
    };

    public string HeaderTitle { get; init; } = "ACCEPTANCE LETTER";

    public string SubmissionCodeLabel { get; init; } = "Submission Code";

    public string VerificationTitle { get; init; } = "Document Verification";

    public string VerificationCodeLabel { get; init; } = "Verification Code";

    public string VerificationUrlLabel { get; init; } = "Verify";

    public string SignerFallbackDuty { get; init; } = "Chairman of the Organizing Committee";

    public IReadOnlyList<string>? BodyParagraphs { get; init; }
}