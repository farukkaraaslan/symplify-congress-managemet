using Core.Application.Pipelines.Authorization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Features.MailDeliveries.Constants;
using Symplify.BackOffice.Application.Features.MailDeliveries.Dtos;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Communication;
using Symplify.BackOffice.Domain.Enums;
using Domain = Symplify.BackOffice.Domain;

namespace Symplify.BackOffice.Application.Features.MailDeliveries.Queries.GetList;

public sealed class GetMailDeliveryListQuery : IRequest<GetMailDeliveryListResponse>, ISecuredRequest
{
    public Guid? OrganizationId { get; set; }
    public Guid? CongressId { get; set; }
    public MailMessageType? MailType { get; set; }
    public MailOutboxStatus? Status { get; set; }
    public MailDeliveryStatus? DeliveryStatus { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? Search { get; set; }

    public string SortColumn { get; set; } = "createdDate";
    public string SortDirection { get; set; } = "desc";

    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 25;

    public Guid? CurrentUserId { get; set; }
    public bool IsSuperAdmin { get; set; }

    public string[] Roles =>
    [
        MailDeliveriesOperationClaims.Admin,
        MailDeliveriesOperationClaims.Read
    ];

    public sealed class Handler : IRequestHandler<GetMailDeliveryListQuery, GetMailDeliveryListResponse>
    {
        private readonly IMailOutboxMessageRepository _outboxRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IOrganizationUserRepository _organizationUserRepository;
        private readonly ICongressRepository _congressRepository;
        private readonly ISubmissionRepository _submissionRepository;

        public Handler(
            IMailOutboxMessageRepository outboxRepository,
            IOrganizationRepository organizationRepository,
            IOrganizationUserRepository organizationUserRepository,
            ICongressRepository congressRepository,
            ISubmissionRepository submissionRepository)
        {
            _outboxRepository = outboxRepository;
            _organizationRepository = organizationRepository;
            _organizationUserRepository = organizationUserRepository;
            _congressRepository = congressRepository;
            _submissionRepository = submissionRepository;
        }

        public async Task<GetMailDeliveryListResponse> Handle(
            GetMailDeliveryListQuery request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<Guid> allowedOrganizationIds = await ResolveAllowedOrganizationIdsAsync(
                request,
                cancellationToken);

            if (!request.IsSuperAdmin && allowedOrganizationIds.Count == 0)
                return Empty(request);

            IQueryable<MailOutboxMessage> scopedQuery = _outboxRepository
                .Query()
                .AsNoTracking();

            if (!request.IsSuperAdmin)
            {
                scopedQuery = scopedQuery.Where(message =>
                    message.OrganizationId.HasValue &&
                    allowedOrganizationIds.Contains(message.OrganizationId.Value));
            }

            int recordsTotalCount = await scopedQuery.CountAsync(cancellationToken);

            IQueryable<MailOutboxMessage> filteredQuery = ApplyFilters(scopedQuery, request);

            int filteredCount = await filteredQuery.CountAsync(cancellationToken);

            // KPI kartları ekranda seçili filtrelerin sonucunu göstermeli.
            int pendingTransportCount = await filteredQuery.CountAsync(
                item => item.Status == MailOutboxStatus.Pending || item.Status == MailOutboxStatus.Processing,
                cancellationToken);

            int failedTransportCount = await filteredQuery.CountAsync(
                item => item.Status == MailOutboxStatus.Failed,
                cancellationToken);

            int deliveredCount = await filteredQuery.CountAsync(
                item => item.DeliveryStatus == MailDeliveryStatus.Delivered,
                cancellationToken);

            int bouncedCount = await filteredQuery.CountAsync(
                item => item.DeliveryStatus == MailDeliveryStatus.Bounced,
                cancellationToken);

            int delayedCount = await filteredQuery.CountAsync(
                item => item.DeliveryStatus == MailDeliveryStatus.Delayed,
                cancellationToken);

            int pageSize = Math.Clamp(request.PageSize, 10, 200);
            int pageIndex = Math.Max(1, request.PageIndex);
            int totalPages = filteredCount == 0
                ? 0
                : (int)Math.Ceiling(filteredCount / (double)pageSize);

            if (totalPages > 0 && pageIndex > totalPages)
                pageIndex = totalPages;

            IQueryable<MailOutboxMessage> orderedQuery = ApplyOrdering(
                filteredQuery,
                request.SortColumn,
                request.SortDirection);

            List<MailOutboxMessage> rows = await orderedQuery
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            Dictionary<Guid, string> organizationNames = await LoadOrganizationNamesAsync(rows, cancellationToken);
            Dictionary<Guid, string> congressNames = await LoadCongressNamesAsync(rows, cancellationToken);
            Dictionary<Guid, string> submissionNumbers = await LoadSubmissionNumbersAsync(rows, cancellationToken);

            IReadOnlyList<MailDeliveryFilterOptionDto> organizations = await LoadOrganizationOptionsAsync(
                request.IsSuperAdmin,
                allowedOrganizationIds,
                cancellationToken);

            IReadOnlyList<MailDeliveryFilterOptionDto> congresses = await LoadCongressOptionsAsync(
                request.IsSuperAdmin,
                allowedOrganizationIds,
                request.OrganizationId,
                cancellationToken);

            return new GetMailDeliveryListResponse
            {
                RecordsTotalCount = recordsTotalCount,
                TotalCount = filteredCount,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalPages = totalPages,
                PendingTransportCount = pendingTransportCount,
                FailedTransportCount = failedTransportCount,
                DeliveredCount = deliveredCount,
                BouncedCount = bouncedCount,
                DelayedCount = delayedCount,
                Organizations = organizations,
                Congresses = congresses,
                Items = rows.Select(row => new MailDeliveryListItemDto
                {
                    Id = row.Id,
                    MailType = row.MailType,
                    RecipientName = row.ToName ?? string.Empty,
                    RecipientEmail = row.ToEmail,
                    Subject = row.Subject,
                    OrganizationId = row.OrganizationId,
                    OrganizationName = row.OrganizationId.HasValue &&
                                       organizationNames.TryGetValue(row.OrganizationId.Value, out string? organizationName)
                        ? organizationName
                        : string.Empty,
                    CongressId = row.CongressId,
                    CongressName = row.CongressId.HasValue &&
                                   congressNames.TryGetValue(row.CongressId.Value, out string? congressName)
                        ? congressName
                        : string.Empty,
                    RelatedUserId = row.RelatedUserId,
                    RelatedSubmissionId = row.RelatedSubmissionId,
                    SubmissionNumber = row.RelatedSubmissionId.HasValue &&
                                       submissionNumbers.TryGetValue(row.RelatedSubmissionId.Value, out string? submissionNumber)
                        ? submissionNumber
                        : null,
                    Status = row.Status,
                    DeliveryStatus = row.DeliveryStatus,
                    Provider = row.Provider,
                    CreatedAt = row.CreatedDate,
                    SentAt = row.SentAt,
                    DeliveredAt = row.DeliveredAt,
                    LastError = row.LastError,
                    DeliveryDiagnosticCode = row.DeliveryDiagnosticCode
                }).ToList()
            };
        }

        private IQueryable<MailOutboxMessage> ApplyFilters(
            IQueryable<MailOutboxMessage> query,
            GetMailDeliveryListQuery request)
        {
            if (request.OrganizationId.HasValue && request.OrganizationId.Value != Guid.Empty)
                query = query.Where(item => item.OrganizationId == request.OrganizationId.Value);

            if (request.CongressId.HasValue && request.CongressId.Value != Guid.Empty)
                query = query.Where(item => item.CongressId == request.CongressId.Value);

            if (request.MailType.HasValue && Enum.IsDefined(request.MailType.Value))
                query = query.Where(item => item.MailType == request.MailType.Value);

            if (request.Status.HasValue && Enum.IsDefined(request.Status.Value))
                query = query.Where(item => item.Status == request.Status.Value);

            if (request.DeliveryStatus.HasValue && Enum.IsDefined(request.DeliveryStatus.Value))
                query = query.Where(item => item.DeliveryStatus == request.DeliveryStatus.Value);

            if (request.DateFrom.HasValue)
            {
                DateTime dateFrom = DateTime.SpecifyKind(request.DateFrom.Value.Date, DateTimeKind.Utc);
                query = query.Where(item => item.CreatedDate >= dateFrom);
            }

            if (request.DateTo.HasValue)
            {
                DateTime dateToExclusive = DateTime.SpecifyKind(
                    request.DateTo.Value.Date.AddDays(1),
                    DateTimeKind.Utc);

                query = query.Where(item => item.CreatedDate < dateToExclusive);
            }

            string? search = NormalizeSearch(request.Search);
            if (string.IsNullOrWhiteSpace(search))
                return query;

            IQueryable<Guid> matchingSubmissionIds = _submissionRepository
                .Query()
                .AsNoTracking()
                .Where(submission => submission.SubmissionNumber.ToLower().Contains(search))
                .Select(submission => submission.Id);

            return query.Where(item =>
                item.ToEmail.ToLower().Contains(search) ||
                (item.ToName != null && item.ToName.ToLower().Contains(search)) ||
                item.Subject.ToLower().Contains(search) ||
                (item.RelatedSubmissionId.HasValue && matchingSubmissionIds.Contains(item.RelatedSubmissionId.Value)));
        }

        private static IQueryable<MailOutboxMessage> ApplyOrdering(
            IQueryable<MailOutboxMessage> query,
            string? sortColumn,
            string? sortDirection)
        {
            bool desc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            string column = string.IsNullOrWhiteSpace(sortColumn)
                ? "createdDate"
                : sortColumn.Trim();

            return column.ToLowerInvariant() switch
            {
                "mailtype" => desc
                    ? query.OrderByDescending(item => item.MailType).ThenByDescending(item => item.CreatedDate)
                    : query.OrderBy(item => item.MailType).ThenByDescending(item => item.CreatedDate),

                "recipient" => desc
                    ? query.OrderByDescending(item => item.ToName ?? item.ToEmail).ThenByDescending(item => item.CreatedDate)
                    : query.OrderBy(item => item.ToName ?? item.ToEmail).ThenByDescending(item => item.CreatedDate),

                "subject" => desc
                    ? query.OrderByDescending(item => item.Subject).ThenByDescending(item => item.CreatedDate)
                    : query.OrderBy(item => item.Subject).ThenByDescending(item => item.CreatedDate),

                "status" => desc
                    ? query.OrderByDescending(item => item.Status).ThenByDescending(item => item.CreatedDate)
                    : query.OrderBy(item => item.Status).ThenByDescending(item => item.CreatedDate),

                "deliverystatus" => desc
                    ? query.OrderByDescending(item => item.DeliveryStatus).ThenByDescending(item => item.CreatedDate)
                    : query.OrderBy(item => item.DeliveryStatus).ThenByDescending(item => item.CreatedDate),

                "sentat" => desc
                    ? query.OrderByDescending(item => item.SentAt).ThenByDescending(item => item.CreatedDate)
                    : query.OrderBy(item => item.SentAt).ThenByDescending(item => item.CreatedDate),

                _ => desc
                    ? query.OrderByDescending(item => item.CreatedDate).ThenByDescending(item => item.Id)
                    : query.OrderBy(item => item.CreatedDate).ThenBy(item => item.Id)
            };
        }

        private async Task<IReadOnlyList<Guid>> ResolveAllowedOrganizationIdsAsync(
            GetMailDeliveryListQuery request,
            CancellationToken cancellationToken)
        {
            if (request.IsSuperAdmin)
                return Array.Empty<Guid>();

            if (!request.CurrentUserId.HasValue || request.CurrentUserId.Value == Guid.Empty)
                return Array.Empty<Guid>();

            return await _organizationUserRepository.Query()
                .AsNoTracking()
                .Where(item =>
                    item.UserId == request.CurrentUserId.Value &&
                    item.IsActive &&
                    item.DeletedDate == null)
                .Select(item => item.OrganizationId)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        private async Task<Dictionary<Guid, string>> LoadOrganizationNamesAsync(
            IReadOnlyCollection<MailOutboxMessage> rows,
            CancellationToken cancellationToken)
        {
            List<Guid> ids = rows
                .Where(item => item.OrganizationId.HasValue)
                .Select(item => item.OrganizationId!.Value)
                .Distinct()
                .ToList();

            if (ids.Count == 0)
                return new Dictionary<Guid, string>();

            return await _organizationRepository.Query()
                .AsNoTracking()
                .Where(item => ids.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        }

        private async Task<Dictionary<Guid, string>> LoadCongressNamesAsync(
            IReadOnlyCollection<MailOutboxMessage> rows,
            CancellationToken cancellationToken)
        {
            List<Guid> ids = rows
                .Where(item => item.CongressId.HasValue)
                .Select(item => item.CongressId!.Value)
                .Distinct()
                .ToList();

            if (ids.Count == 0)
                return new Dictionary<Guid, string>();

            return await _congressRepository.Query()
                .AsNoTracking()
                .Where(item => ids.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        }

        private async Task<Dictionary<Guid, string>> LoadSubmissionNumbersAsync(
            IReadOnlyCollection<MailOutboxMessage> rows,
            CancellationToken cancellationToken)
        {
            List<Guid> ids = rows
                .Where(item => item.RelatedSubmissionId.HasValue)
                .Select(item => item.RelatedSubmissionId!.Value)
                .Distinct()
                .ToList();

            if (ids.Count == 0)
                return new Dictionary<Guid, string>();

            return await _submissionRepository.Query()
                .AsNoTracking()
                .Where(item => ids.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, item => item.SubmissionNumber, cancellationToken);
        }

        private async Task<IReadOnlyList<MailDeliveryFilterOptionDto>> LoadOrganizationOptionsAsync(
            bool isSuperAdmin,
            IReadOnlyList<Guid> allowedOrganizationIds,
            CancellationToken cancellationToken)
        {
            IQueryable<Domain.Organization.Organization> query = _organizationRepository
                .Query()
                .AsNoTracking();

            if (!isSuperAdmin)
                query = query.Where(item => allowedOrganizationIds.Contains(item.Id));

            return await query
                .OrderBy(item => item.Name)
                .Select(item => new MailDeliveryFilterOptionDto
                {
                    Id = item.Id,
                    Name = item.Name
                })
                .ToListAsync(cancellationToken);
        }

        private async Task<IReadOnlyList<MailDeliveryFilterOptionDto>> LoadCongressOptionsAsync(
            bool isSuperAdmin,
            IReadOnlyList<Guid> allowedOrganizationIds,
            Guid? organizationId,
            CancellationToken cancellationToken)
        {
            IQueryable<Domain.Congress.Congress> query = _congressRepository
                .Query()
                .AsNoTracking();

            if (!isSuperAdmin)
                query = query.Where(item => allowedOrganizationIds.Contains(item.OrganizationId));

            if (organizationId.HasValue && organizationId.Value != Guid.Empty)
                query = query.Where(item => item.OrganizationId == organizationId.Value);

            return await query
                .OrderByDescending(item => item.StartDate)
                .ThenBy(item => item.Name)
                .Select(item => new MailDeliveryFilterOptionDto
                {
                    Id = item.Id,
                    Name = item.Name
                })
                .ToListAsync(cancellationToken);
        }

        private static string? NormalizeSearch(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            string normalized = value.Trim().ToLowerInvariant();
            return normalized.Length <= 200
                ? normalized
                : normalized[..200];
        }

        private static GetMailDeliveryListResponse Empty(GetMailDeliveryListQuery request)
            => new()
            {
                RecordsTotalCount = 0,
                TotalCount = 0,
                PageIndex = Math.Max(1, request.PageIndex),
                PageSize = Math.Clamp(request.PageSize, 10, 200)
            };
    }
}
