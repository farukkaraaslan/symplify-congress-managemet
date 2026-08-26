using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Localization;

namespace Symplify.BackOffice.Application.Services.Mailing;

public sealed class SystemMailTemplateRenderer : ISystemMailTemplateRenderer
{
    private readonly IResourceValueRepository _resourceValueRepository;
    private readonly ILanguageRepository _languageRepository;
    private readonly MailTemplateOptions _options;

    public SystemMailTemplateRenderer(
        IResourceValueRepository resourceValueRepository,
        ILanguageRepository languageRepository,
        IOptions<MailTemplateOptions> options)
    {
        _resourceValueRepository = resourceValueRepository;
        _languageRepository = languageRepository;
        _options = options.Value;
    }

    public async Task<RenderedSystemMailTemplate> RenderAsync(
        SystemMailTemplateRenderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Dictionary<string, string?> tokens = new(request.Tokens, StringComparer.OrdinalIgnoreCase)
        {
            ["BrandName"] = ResolveBrandName(request),
            ["ContextTitle"] = request.Branding.ContextTitle
        };

        string subject = ReplaceTokens(
            await LocalizeAsync(request.SubjectKey, request.LanguageId, request.Culture, cancellationToken),
            tokens,
            encodeHtml: false);

        string title = ReplaceTokens(
            await LocalizeAsync(request.TitleKey, request.LanguageId, request.Culture, cancellationToken),
            tokens,
            encodeHtml: true);

        string body = ReplaceTokens(
            await LocalizeAsync(request.BodyKey, request.LanguageId, request.Culture, cancellationToken),
            tokens,
            encodeHtml: true)
            .Replace("\n", "<br />", StringComparison.Ordinal);

        string? actionText = null;
        if (!string.IsNullOrWhiteSpace(request.ActionTextKey))
        {
            actionText = ReplaceTokens(
                await LocalizeAsync(request.ActionTextKey, request.LanguageId, request.Culture, cancellationToken),
                tokens,
                encodeHtml: true);
        }

        string footer = ReplaceTokens(
            await LocalizeAsync(SystemMailResourceKeys.CommonFooter, request.LanguageId, request.Culture, cancellationToken),
            tokens,
            encodeHtml: true);

        string ifNotRequested = ReplaceTokens(
            await LocalizeAsync(SystemMailResourceKeys.CommonIfNotRequested, request.LanguageId, request.Culture, cancellationToken),
            tokens,
            encodeHtml: true);

        string openInBrowserFallback = ReplaceTokens(
            await LocalizeAsync(SystemMailResourceKeys.CommonOpenInBrowserFallback, request.LanguageId, request.Culture, cancellationToken),
            tokens,
            encodeHtml: true);

        string htmlBody = BuildHtml(
            title: title,
            body: body,
            actionText: actionText,
            actionUrl: request.ActionUrl,
            infoRows: request.InfoRows,
            branding: request.Branding,
            footer: footer,
            ifNotRequested: ifNotRequested,
            openInBrowserFallback: openInBrowserFallback,
            showIfNotRequestedMessage: request.ShowIfNotRequestedMessage,
            culture: request.Culture);

        return new RenderedSystemMailTemplate
        {
            Subject = subject,
            HtmlBody = htmlBody
        };
    }

    public async Task<RenderedSystemMailTemplate> RenderCustomAsync(
        CustomMailTemplateRenderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string subject = NormalizeHeader(request.Subject);
        string title = Html((request.Title ?? string.Empty).Trim());
        string footer = Html(await LocalizeAsync(
            SystemMailResourceKeys.CommonFooter,
            request.LanguageId,
            request.Culture,
            cancellationToken));

        string htmlBody = BuildHtml(
            title: title,
            body: request.SafeBodyHtml,
            actionText: null,
            actionUrl: null,
            infoRows: Array.Empty<MailInfoRowModel>(),
            branding: request.Branding,
            footer: footer,
            ifNotRequested: string.Empty,
            openInBrowserFallback: string.Empty,
            showIfNotRequestedMessage: false,
            culture: request.Culture);

        return new RenderedSystemMailTemplate
        {
            Subject = subject,
            HtmlBody = htmlBody
        };
    }

    private async Task<string> LocalizeAsync(
        string key,
        Guid? languageId,
        string? culture,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        ResourceValue? resourceValue = null;

        if (languageId.HasValue && languageId.Value != Guid.Empty)
        {
            resourceValue = await _resourceValueRepository.GetAsync(
                predicate: value =>
                    value.ResourceKey.KeyName == key &&
                    value.LanguageId == languageId.Value,
                cancellationToken: cancellationToken);
        }

        if (resourceValue is null && !string.IsNullOrWhiteSpace(culture))
        {
            string normalizedCulture = culture.Trim();
            resourceValue = await _resourceValueRepository.GetAsync(
                predicate: value =>
                    value.ResourceKey.KeyName == key &&
                    value.Language.Culture == normalizedCulture,
                cancellationToken: cancellationToken);
        }

        if (resourceValue is not null && !string.IsNullOrWhiteSpace(resourceValue.Value))
            return resourceValue.Value;

        Language? defaultLanguage = await _languageRepository.GetAsync(
            predicate: language => language.IsDefault && language.IsActive,
            cancellationToken: cancellationToken);

        if (defaultLanguage is not null)
        {
            resourceValue = await _resourceValueRepository.GetAsync(
                predicate: value =>
                    value.ResourceKey.KeyName == key &&
                    value.LanguageId == defaultLanguage.Id,
                cancellationToken: cancellationToken);

            if (resourceValue is not null && !string.IsNullOrWhiteSpace(resourceValue.Value))
                return resourceValue.Value;
        }

        return key;
    }

    private static string BuildHtml(
        string title,
        string body,
        string? actionText,
        string? actionUrl,
        IList<MailInfoRowModel> infoRows,
        MailBrandingModel branding,
        string footer,
        string ifNotRequested,
        string openInBrowserFallback,
        bool showIfNotRequestedMessage,
        string? culture)
    {
        string htmlLang = ResolveHtmlLang(culture);
        string safeBrandName = Html(string.IsNullOrWhiteSpace(branding.BrandName) ? "Symplify" : branding.BrandName.Trim());
        string safeContextTitle = Html((branding.ContextTitle ?? string.Empty).Trim());
        string? safeLogoContentId = string.IsNullOrWhiteSpace(branding.LogoContentId)
            ? null
            : HtmlAttribute(branding.LogoContentId);
        string safeLogoAltText = HtmlAttribute(
            string.IsNullOrWhiteSpace(branding.LogoAltText)
                ? branding.BrandName
                : branding.LogoAltText);
        string? safeActionUrl = string.IsNullOrWhiteSpace(actionUrl) ? null : HtmlAttribute(actionUrl);
        string footerBrandText = !string.IsNullOrWhiteSpace(branding.ContextTitle)
            ? safeContextTitle
            : safeBrandName;

        StringBuilder rowsBuilder = new();
        foreach (MailInfoRowModel row in infoRows.Where(row => !string.IsNullOrWhiteSpace(row.Value)))
        {
            rowsBuilder.Append($"""
                <tr>
                    <td style="padding:12px 0;border-bottom:1px solid #e9eef5;color:#667085;font-size:13px;line-height:20px;">{Html(row.Label)}</td>
                    <td style="padding:12px 0;border-bottom:1px solid #e9eef5;color:#101828;font-size:13px;line-height:20px;font-weight:600;text-align:right;">{Html(row.Value)}</td>
                </tr>
                """);
        }

        string rowsHtml = rowsBuilder.Length == 0
            ? string.Empty
            : $"""
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin:26px 0 8px;border-collapse:collapse;">
                    {rowsBuilder}
                </table>
                """;

        string buttonHtml = string.Empty;
        if (!string.IsNullOrWhiteSpace(actionText) && !string.IsNullOrWhiteSpace(safeActionUrl))
        {
            string displayedUrl = Html(actionUrl ?? string.Empty);

            buttonHtml = $"""
                <div style="text-align:center;margin:32px 0 18px;">
                    <a href="{safeActionUrl}" style="display:inline-block;background:#2563eb;color:#ffffff;text-decoration:none;border-radius:14px;padding:16px 34px;font-size:16px;font-weight:700;line-height:22px;box-shadow:0 10px 26px rgba(37,99,235,.24);">{actionText}</a>
                </div>
                <div style="margin:18px 0 0;text-align:center;color:#667085;font-size:13px;line-height:20px;">{openInBrowserFallback}</div>
                <div style="margin:10px auto 0;max-width:520px;background:#f8fbff;border:1px solid #dbe7f3;border-radius:14px;padding:14px 16px;text-align:left;word-break:break-all;">
                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse;">
                        <tr>
                            <td width="28" valign="top" style="font-size:18px;line-height:20px;color:#2563eb;padding-right:8px;">&#128279;</td>
                            <td style="font-size:13px;line-height:20px;">
                                <a href="{safeActionUrl}" style="color:#2563eb;text-decoration:none;">{displayedUrl}</a>
                            </td>
                        </tr>
                    </table>
                </div>
                """;
        }

        string ifNotRequestedHtml = showIfNotRequestedMessage
            ? $"""
                <div style="margin:24px 0 0;padding:14px 16px;background:#f8fafc;border:1px solid #e8eef5;border-radius:14px;color:#475467;font-size:13px;line-height:20px;text-align:left;">
                    {ifNotRequested}
                </div>
                """
            : string.Empty;

        string logoHtml = safeLogoContentId is null
            ? $"<div style=\"font-size:28px;font-weight:800;color:#0f285f;text-align:center;letter-spacing:-.3px;\">{safeBrandName}</div>"
            : $"<img src=\"cid:{safeLogoContentId}\" width=\"132\" alt=\"{safeLogoAltText}\" style=\"display:block;margin:0 auto;max-width:132px;height:auto;border:0;outline:none;text-decoration:none;\" />";

        string contextTitleHtml = string.IsNullOrWhiteSpace(branding.ContextTitle)
            ? string.Empty
            : $"<div style=\"margin-top:10px;color:#2b63c6;font-size:12px;line-height:17px;font-weight:700;text-align:center;letter-spacing:.2px;\">{safeContextTitle}</div>";

        int year = DateTime.UtcNow.Year;

        return $"""
            <!doctype html>
            <html lang="{htmlLang}">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>{title}</title>
            </head>
            <body style="margin:0;padding:0;background:#f4f7fb;font-family:Arial,Helvetica,sans-serif;color:#101828;">
              <div style="display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;">{title}</div>
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f4f7fb;border-collapse:collapse;">
                <tr>
                  <td align="center" style="padding:28px 12px;">
                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:680px;border-collapse:collapse;">
                      <tr>
                        <td>
                          <div style="background:#ffffff;border:1px solid #dce5f0;border-radius:28px;padding:28px 32px 20px;box-shadow:0 20px 44px rgba(16,24,40,.07);">
                            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse;">
                              <tr>
                                <td width="36%" valign="middle">
                                  <div style="height:1px;background:#dfe7f1;"></div>
                                </td>
                                <td width="28%" align="center" valign="middle" style="padding:0 14px;">
                                  {logoHtml}
                                  {contextTitleHtml}
                                </td>
                                <td width="36%" valign="middle">
                                  <div style="height:1px;background:#dfe7f1;"></div>
                                </td>
                              </tr>
                            </table>

                            <h1 style="margin:34px 0 14px;color:#0f285f;font-size:24px;line-height:32px;font-weight:800;text-align:center;letter-spacing:-.3px;">{title}</h1>
                            <div style="margin:0;color:#475467;font-size:16px;line-height:26px;text-align:center;">
                              {body}
                            </div>
                            {rowsHtml}
                            {buttonHtml}
                            {ifNotRequestedHtml}

                            <div style="margin-top:28px;background:#f8fbff;border:1px solid #e5edf6;border-radius:16px;padding:14px 16px;">
                              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse;">
                                <tr>
                                  <td valign="middle" style="color:#667085;font-size:12px;line-height:18px;">
                                    <span style="font-size:14px;line-height:18px;padding-right:6px;">&#128737;</span>{footer}
                                  </td>
                                  <td align="right" valign="middle" style="color:#98a2b3;font-size:12px;line-height:18px;white-space:nowrap;">
                                    &copy; {year} {footerBrandText}
                                  </td>
                                </tr>
                              </table>
                            </div>
                          </div>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }

    private static string ReplaceTokens(string value, IDictionary<string, string?> tokens, bool encodeHtml)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        string result = value;
        foreach (KeyValuePair<string, string?> token in tokens)
        {
            string replacement = token.Value ?? string.Empty;
            if (encodeHtml)
                replacement = Html(replacement);

            result = result.Replace("{{" + token.Key + "}}", replacement, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    private static string NormalizeHeader(string value)
    {
        return (value ?? string.Empty)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();
    }

    private static string ResolveHtmlLang(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
            return "tr";

        string normalizedCulture = culture.Trim();
        int separatorIndex = normalizedCulture.IndexOf('-');
        return separatorIndex > 0
            ? normalizedCulture[..separatorIndex].ToLowerInvariant()
            : normalizedCulture.ToLowerInvariant();
    }

    private string ResolveBrandName(SystemMailTemplateRenderRequest request)
    {
        return string.IsNullOrWhiteSpace(request.Branding.BrandName)
            ? (string.IsNullOrWhiteSpace(_options.BrandName) ? "Symplify" : _options.BrandName.Trim())
            : request.Branding.BrandName.Trim();
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private static string HtmlAttribute(string value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
