using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.BulkEmails.Constants;
using Symplify.BackOffice.Application.Features.BulkEmails.Dtos;
using Symplify.BackOffice.Application.Features.BulkEmails.Rules;
using Symplify.BackOffice.Application.Features.BulkEmails.Services;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.BulkEmails.Queries.PreviewRecipients;

public sealed class PreviewBulkEmailRecipientsQuery : IRequest<PreviewBulkEmailRecipientsResponse>, ISecuredRequest
{
    public Guid CongressId { get; set; }

    public BulkEmailAudienceType AudienceType { get; set; }

    public Guid? CurrentUserId { get; set; }

    public bool IsSuperAdmin { get; set; }

    public int PageIndex { get; set; } = 1;

    public int PageSize { get; set; } = 25;

    public string? Search { get; set; }

    public IReadOnlyCollection<string> ExcludedRecipientEmails { get; set; } = Array.Empty<string>();

    public IReadOnlyCollection<BulkEmailRecipientDto> AdditionalRecipients { get; set; } = Array.Empty<BulkEmailRecipientDto>();

    public string[] Roles => [BulkEmailsOperationClaims.Admin, BulkEmailsOperationClaims.Read];

    public sealed class Handler : IRequestHandler<PreviewBulkEmailRecipientsQuery, PreviewBulkEmailRecipientsResponse>
    {
        private readonly BulkEmailBusinessRules _rules;
        private readonly IBulkEmailRecipientResolver _recipientResolver;

        public Handler(
            BulkEmailBusinessRules rules,
            IBulkEmailRecipientResolver recipientResolver)
        {
            _rules = rules;
            _recipientResolver = recipientResolver;
        }

        public async Task<PreviewBulkEmailRecipientsResponse> Handle(
            PreviewBulkEmailRecipientsQuery request,
            CancellationToken cancellationToken)
        {
            await _rules.GetAuthorizedCongressAsync(
                request.CongressId,
                request.CurrentUserId,
                request.IsSuperAdmin,
                cancellationToken);

            BulkEmailRecipientResolutionResult result = await _recipientResolver.ResolveAdjustedAsync(
                request.CongressId,
                request.AudienceType,
                request.ExcludedRecipientEmails,
                request.AdditionalRecipients,
                cancellationToken);

            IEnumerable<BulkEmailRecipientDto> filteredRecipients = result.Recipients;
            string search = request.Search?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(search))
            {
                filteredRecipients = filteredRecipients.Where(recipient =>
                    recipient.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                    recipient.Email.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            List<BulkEmailRecipientDto> filteredList = filteredRecipients.ToList();
            int pageSize = Math.Clamp(request.PageSize, 10, 100);
            int totalPages = Math.Max(1, (int)Math.Ceiling(filteredList.Count / (double)pageSize));
            int pageIndex = Math.Clamp(request.PageIndex, 1, totalPages);

            return new PreviewBulkEmailRecipientsResponse
            {
                RecipientCount = result.Recipients.Count,
                FilteredCount = filteredList.Count,
                InvalidEmailCount = result.InvalidEmailCount,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalPages = totalPages,
                Recipients = filteredList
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToList()
            };
        }
    }
}
