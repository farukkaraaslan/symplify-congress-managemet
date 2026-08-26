using System.Net;
using System.Text.RegularExpressions;

namespace Symplify.BackOffice.Application.Features.Submissions.Helpers;

public static class SubmissionHtmlTextLengthHelper
{
    public static int GetPlainTextLength(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return 0;

        string decoded = WebUtility.HtmlDecode(html);

        string withoutScriptAndStyle = Regex.Replace(
            decoded,
            @"<(script|style)\b[^>]*>[\s\S]*?</\1>",
            " ",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        string withLineBreaks = Regex.Replace(
            withoutScriptAndStyle,
            @"<\s*(br|/p|/div|/li|/tr)\s*/?>",
            " ",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        string withoutTags = Regex.Replace(
            withLineBreaks,
            @"<[^>]+>",
            " ",
            RegexOptions.CultureInvariant);

        string decodedText = WebUtility.HtmlDecode(withoutTags);
        string normalized = Regex.Replace(decodedText, @"\s+", " ").Trim();

        return normalized.Length;
    }

    public static bool IsWithinPlainTextLimit(string? html, int maxPlainTextLength)
    {
        return GetPlainTextLength(html) <= maxPlainTextLength;
    }
}
