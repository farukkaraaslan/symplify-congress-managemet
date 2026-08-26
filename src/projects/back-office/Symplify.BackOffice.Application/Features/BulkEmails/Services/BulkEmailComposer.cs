using System.Net;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.BulkEmails.Constants;
using Symplify.BackOffice.Application.Features.BulkEmails.Dtos;
using Symplify.BackOffice.Application.Services.Mailing;

namespace Symplify.BackOffice.Application.Features.BulkEmails.Services;

public sealed class BulkEmailComposer : IBulkEmailComposer
{
    private const string RecipientPlaceholder = "__SYMPLIFY_BULK_RECIPIENT_7B4203E6__";

    private readonly IBulkEmailBodyRenderer _bodyRenderer;
    private readonly IMailBrandingResolver _brandingResolver;
    private readonly ISystemMailTemplateRenderer _templateRenderer;

    public BulkEmailComposer(
        IBulkEmailBodyRenderer bodyRenderer,
        IMailBrandingResolver brandingResolver,
        ISystemMailTemplateRenderer templateRenderer)
    {
        _bodyRenderer = bodyRenderer;
        _brandingResolver = brandingResolver;
        _templateRenderer = templateRenderer;
    }

    public async Task<PreparedBulkEmailTemplate> PrepareAsync(
        Guid congressId,
        string? culture,
        string subject,
        string title,
        string bodyText,
        CancellationToken cancellationToken = default)
    {
        MailBrandingModel branding = await _brandingResolver.ResolveForCongressAsync(
            congressId,
            culture: culture,
            cancellationToken: cancellationToken);

        string congressTitle = string.IsNullOrWhiteSpace(branding.ContextTitle)
            ? branding.BrandName
            : branding.ContextTitle;

        string preparedSubject = ReplaceCommonTokens(subject, congressTitle);
        string preparedTitle = ReplaceCommonTokens(title, congressTitle);
        string preparedBody = ReplaceCommonTokens(bodyText, congressTitle);

        BulkEmailBodyRenderResult bodyResult = _bodyRenderer.Render(preparedBody);
        if (bodyResult.UnsafeLinks.Count > 0)
            throw new BusinessException(BulkEmailsMessages.UnsafeLinksDetected);

        RenderedSystemMailTemplate rendered = await _templateRenderer.RenderCustomAsync(
            new CustomMailTemplateRenderRequest
            {
                Culture = culture,
                Subject = preparedSubject,
                Title = preparedTitle,
                SafeBodyHtml = bodyResult.Html,
                Branding = branding
            },
            cancellationToken);

        return new PreparedBulkEmailTemplate
        {
            SubjectTemplate = rendered.Subject,
            HtmlBodyTemplate = rendered.HtmlBody,
            RecipientPlaceholder = RecipientPlaceholder,
            CongressTitle = congressTitle,
            WarningLinks = bodyResult.WarningLinks
        };
    }

    public string RenderSubject(PreparedBulkEmailTemplate template, string recipientName)
    {
        string safeRecipientName = (recipientName ?? string.Empty)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();

        return template.SubjectTemplate.Replace(
            template.RecipientPlaceholder,
            safeRecipientName,
            StringComparison.Ordinal);
    }

    public string RenderHtmlBody(PreparedBulkEmailTemplate template, string recipientName)
    {
        return template.HtmlBodyTemplate.Replace(
            template.RecipientPlaceholder,
            WebUtility.HtmlEncode(recipientName ?? string.Empty),
            StringComparison.Ordinal);
    }

    private static string ReplaceCommonTokens(string value, string congressTitle)
    {
        return (value ?? string.Empty)
            .Replace("{{CongressTitle}}", congressTitle ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{{RecipientName}}", RecipientPlaceholder, StringComparison.OrdinalIgnoreCase);
    }
}
