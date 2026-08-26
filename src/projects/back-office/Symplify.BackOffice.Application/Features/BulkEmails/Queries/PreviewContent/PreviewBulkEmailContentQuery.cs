using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.BulkEmails.Constants;
using Symplify.BackOffice.Application.Features.BulkEmails.Dtos;
using Symplify.BackOffice.Application.Features.BulkEmails.Rules;
using Symplify.BackOffice.Application.Features.BulkEmails.Services;

namespace Symplify.BackOffice.Application.Features.BulkEmails.Queries.PreviewContent;

public sealed class PreviewBulkEmailContentQuery : IRequest<PreviewBulkEmailContentResponse>, ISecuredRequest
{
    public Guid CongressId { get; set; }

    public string Culture { get; set; } = "tr-TR";

    public string Subject { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string BodyText { get; set; } = string.Empty;

    public Guid? CurrentUserId { get; set; }

    public bool IsSuperAdmin { get; set; }

    public string[] Roles => [BulkEmailsOperationClaims.Admin, BulkEmailsOperationClaims.Read];

    public sealed class Handler : IRequestHandler<PreviewBulkEmailContentQuery, PreviewBulkEmailContentResponse>
    {
        private readonly BulkEmailBusinessRules _rules;
        private readonly IBulkEmailBodyRenderer _bodyRenderer;
        private readonly IBulkEmailComposer _composer;

        public Handler(
            BulkEmailBusinessRules rules,
            IBulkEmailBodyRenderer bodyRenderer,
            IBulkEmailComposer composer)
        {
            _rules = rules;
            _bodyRenderer = bodyRenderer;
            _composer = composer;
        }

        public async Task<PreviewBulkEmailContentResponse> Handle(
            PreviewBulkEmailContentQuery request,
            CancellationToken cancellationToken)
        {
            await _rules.GetAuthorizedCongressAsync(
                request.CongressId,
                request.CurrentUserId,
                request.IsSuperAdmin,
                cancellationToken);

            BulkEmailBodyRenderResult safetyResult = _bodyRenderer.Render(request.BodyText);
            if (safetyResult.UnsafeLinks.Count > 0)
            {
                return new PreviewBulkEmailContentResponse
                {
                    CanSend = false,
                    UnsafeLinks = safetyResult.UnsafeLinks,
                    WarningLinks = safetyResult.WarningLinks
                };
            }

            PreparedBulkEmailTemplate template = await _composer.PrepareAsync(
                request.CongressId,
                request.Culture,
                request.Subject,
                request.Title,
                request.BodyText,
                cancellationToken);

            string sampleName = request.Culture.StartsWith("en", StringComparison.OrdinalIgnoreCase)
                ? "Sample Participant"
                : "Örnek Katılımcı";

            return new PreviewBulkEmailContentResponse
            {
                CanSend = true,
                Subject = _composer.RenderSubject(template, sampleName),
                HtmlBody = _composer.RenderHtmlBody(template, sampleName),
                WarningLinks = template.WarningLinks
            };
        }
    }
}
