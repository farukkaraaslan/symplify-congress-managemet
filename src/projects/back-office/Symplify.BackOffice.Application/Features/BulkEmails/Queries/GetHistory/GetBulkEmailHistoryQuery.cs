using Core.Application.Pipelines.Authorization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Features.BulkEmails.Constants;
using Symplify.BackOffice.Application.Features.BulkEmails.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Communication;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.BulkEmails.Queries.GetHistory;

public sealed class GetBulkEmailHistoryQuery : IRequest<GetBulkEmailHistoryResponse>, ISecuredRequest
{
    public Guid CongressId { get; set; }

    public int PageIndex { get; set; } = 1;

    public int PageSize { get; set; } = 25;

    public MailOutboxStatus? Status { get; set; }

    public bool? Opened { get; set; }

    public string? Search { get; set; }

    public Guid? CurrentUserId { get; set; }

    public bool IsSuperAdmin { get; set; }

    public string[] Roles =>
    [
        BulkEmailsOperationClaims.Admin,
        BulkEmailsOperationClaims.Read
    ];

    public sealed class Handler : IRequestHandler<GetBulkEmailHistoryQuery, GetBulkEmailHistoryResponse>
    {
        private readonly BulkEmailBusinessRules _rules;
        private readonly IMailOutboxMessageRepository _outboxRepository;

        public Handler(
            BulkEmailBusinessRules rules,
            IMailOutboxMessageRepository outboxRepository)
        {
            _rules = rules;
            _outboxRepository = outboxRepository;
        }

        public async Task<GetBulkEmailHistoryResponse> Handle(
            GetBulkEmailHistoryQuery request,
            CancellationToken cancellationToken)
        {
            await _rules.GetAuthorizedCongressAsync(
                request.CongressId,
                request.CurrentUserId,
                request.IsSuperAdmin,
                cancellationToken);

            int pageIndex = Math.Max(1, request.PageIndex);
            int pageSize = Math.Clamp(request.PageSize, 10, 100);

            IQueryable<MailOutboxMessage> congressQuery = _outboxRepository
                .Query()
                .AsNoTracking()
                .Where(message =>
                    message.CongressId == request.CongressId &&
                    message.BulkEmailBatchId != null);

            int pendingCount = await congressQuery.CountAsync(
                message => message.Status == MailOutboxStatus.Pending,
                cancellationToken);
            int sentCount = await congressQuery.CountAsync(
                message => message.Status == MailOutboxStatus.Sent,
                cancellationToken);
            int failedCount = await congressQuery.CountAsync(
                message => message.Status == MailOutboxStatus.Failed,
                cancellationToken);
            int cancelledCount = await congressQuery.CountAsync(
                message => message.Status == MailOutboxStatus.Cancelled,
                cancellationToken);
            int openedCount = await congressQuery.CountAsync(
                message => message.FirstOpenedAt != null,
                cancellationToken);

            IQueryable<MailOutboxMessage> filteredQuery = congressQuery;

            if (request.Status.HasValue)
                filteredQuery = filteredQuery.Where(message => message.Status == request.Status.Value);

            if (request.Opened.HasValue)
            {
                filteredQuery = request.Opened.Value
                    ? filteredQuery.Where(message => message.FirstOpenedAt != null)
                    : filteredQuery.Where(message => message.FirstOpenedAt == null);
            }

            string? normalizedSearch = NormalizeSearch(request.Search);
            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                filteredQuery = filteredQuery.Where(message =>
                    message.ToEmail.ToLower().Contains(normalizedSearch) ||
                    (message.ToName != null && message.ToName.ToLower().Contains(normalizedSearch)) ||
                    message.Subject.ToLower().Contains(normalizedSearch));
            }

            int totalCount = await filteredQuery.CountAsync(cancellationToken);
            int totalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling(totalCount / (double)pageSize);

            if (totalPages > 0 && pageIndex > totalPages)
                pageIndex = totalPages;

            List<BulkEmailHistoryItemDto> items = await filteredQuery
                .OrderByDescending(message => message.CreatedDate)
                .ThenBy(message => message.ToEmail)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(message => new BulkEmailHistoryItemDto
                {
                    Id = message.Id,
                    BatchId = message.BulkEmailBatchId!.Value,
                    RecipientName = message.ToName ?? string.Empty,
                    RecipientEmail = message.ToEmail,
                    Subject = message.Subject,
                    AudienceType = message.BulkEmailAudienceType ?? BulkEmailAudienceType.AllRegistered,
                    Status = message.Status,
                    AttemptCount = message.AttemptCount,
                    CreatedAt = message.CreatedDate,
                    SentAt = message.SentAt,
                    FirstOpenedAt = message.FirstOpenedAt,
                    LastOpenedAt = message.LastOpenedAt,
                    OpenCount = message.OpenCount,
                    LastError = message.LastError
                })
                .ToListAsync(cancellationToken);

            return new GetBulkEmailHistoryResponse
            {
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalPages = totalPages,
                PendingCount = pendingCount,
                SentCount = sentCount,
                FailedCount = failedCount,
                CancelledCount = cancelledCount,
                OpenedCount = openedCount,
                Items = items
            };
        }

        private static string? NormalizeSearch(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            string normalized = value.Trim().ToLowerInvariant();
            return normalized.Length <= 200 ? normalized : normalized[..200];
        }
    }
}
