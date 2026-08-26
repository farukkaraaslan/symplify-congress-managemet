using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Symplify.BackOffice.Application.Features.BulkEmails.Dtos;

namespace Symplify.BackOffice.Application.Features.BulkEmails.Services;

public sealed class BulkEmailBodyRenderer : IBulkEmailBodyRenderer
{
    private static readonly Regex LinkRegex = new(
        """(?<url>(?:(?:https?|ftp|file)://|(?:javascript|data|vbscript|mailto):|www\.)[^\s<>"']+)""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly char[] TrailingPunctuation = ['.', ',', ';', ':', '!', '?', ')', ']', '}'];

    public BulkEmailBodyRenderResult Render(string plainText)
    {
        string normalizedText = (plainText ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

        List<string> unsafeLinks = new();
        List<string> warningLinks = new();
        StringBuilder html = new();
        int currentIndex = 0;

        foreach (Match match in LinkRegex.Matches(normalizedText))
        {
            if (match.Index > currentIndex)
                AppendEncodedText(html, normalizedText[currentIndex..match.Index]);

            string matchedValue = match.Groups["url"].Value;
            string coreValue = matchedValue.TrimEnd(TrailingPunctuation);
            string trailingValue = matchedValue[coreValue.Length..];

            if (TryNormalizeSafeUrl(coreValue, out string safeUrl, out bool isHttpWarning))
            {
                if (isHttpWarning)
                    warningLinks.Add(coreValue);

                string encodedUrl = WebUtility.HtmlEncode(safeUrl);
                string encodedText = WebUtility.HtmlEncode(coreValue);

                html.Append($"<a href=\"{encodedUrl}\" target=\"_blank\" rel=\"noopener noreferrer nofollow\" style=\"color:#2563eb;text-decoration:underline;word-break:break-all;\">{encodedText}</a>");
            }
            else
            {
                unsafeLinks.Add(coreValue);
                html.Append(WebUtility.HtmlEncode(coreValue));
            }

            if (!string.IsNullOrEmpty(trailingValue))
                html.Append(WebUtility.HtmlEncode(trailingValue));

            currentIndex = match.Index + match.Length;
        }

        if (currentIndex < normalizedText.Length)
            AppendEncodedText(html, normalizedText[currentIndex..]);

        return new BulkEmailBodyRenderResult
        {
            Html = html.ToString(),
            UnsafeLinks = unsafeLinks
                .Where(link => !string.IsNullOrWhiteSpace(link))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            WarningLinks = warningLinks
                .Where(link => !string.IsNullOrWhiteSpace(link))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    private static void AppendEncodedText(StringBuilder builder, string value)
    {
        string encoded = WebUtility.HtmlEncode(value);
        builder.Append(encoded.Replace("\n", "<br />", StringComparison.Ordinal));
    }

    private static bool TryNormalizeSafeUrl(string value, out string safeUrl, out bool isHttpWarning)
    {
        safeUrl = string.Empty;
        isHttpWarning = false;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        string candidate = value.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? "https://" + value
            : value;

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out Uri? uri))
            return false;

        bool isHttps = uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        bool isHttp = uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);

        if (!isHttps && !isHttp)
            return false;

        if (string.IsNullOrWhiteSpace(uri.Host) || !string.IsNullOrWhiteSpace(uri.UserInfo))
            return false;

        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (System.Net.IPAddress.TryParse(uri.Host, out System.Net.IPAddress? address) && IsPrivateOrLoopback(address))
            return false;

        isHttpWarning = isHttp;
        safeUrl = uri.AbsoluteUri;
        return true;
    }

    private static bool IsPrivateOrLoopback(System.Net.IPAddress address)
    {
        if (System.Net.IPAddress.IsLoopback(address))
            return true;

        byte[] bytes = address.GetAddressBytes();

        if (address.AddressFamily == AddressFamily.InterNetwork && bytes.Length == 4)
        {
            return bytes[0] == 10 ||
                   bytes[0] == 127 ||
                   (bytes[0] == 169 && bytes[1] == 254) ||
                   (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168);
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6 && bytes.Length == 16)
        {
            bool isUniqueLocal = (bytes[0] & 0xFE) == 0xFC;
            return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || isUniqueLocal;
        }

        return false;
    }
}
