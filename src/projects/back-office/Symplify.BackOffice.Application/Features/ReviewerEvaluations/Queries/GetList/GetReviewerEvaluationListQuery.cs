using Core.Application.Pipelines.Authorization;
using Core.Application.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Features.ReviewerEvaluations.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Application.Features.ReviewerEvaluations.Queries.GetList;

public sealed class GetReviewerEvaluationListQuery : IRequest<GetReviewerEvaluationListResponse>, ISecuredRequest
{
    public Guid? CurrentUserId { get; set; }
    public string? Culture { get; set; }
    public PageRequest? PageRequest { get; set; }
    public string? SearchText { get; set; }
    public Guid? CongressId { get; set; }
    public string? Status { get; set; }
    public Guid? TopicId { get; set; }
    public Guid? SubmissionTypeId { get; set; }
    public string? SortColumn { get; set; }
    public string? SortDirection { get; set; }

    public string[] Roles => new[]
    {
        ReviewerEvaluationsOperationClaims.Admin,
        ReviewerEvaluationsOperationClaims.Read,
        ReviewerEvaluationsOperationClaims.Write
    };

    public sealed class Handler : IRequestHandler<GetReviewerEvaluationListQuery, GetReviewerEvaluationListResponse>
    {
        private const int DefaultPage = 0;
        private const int DefaultPageSize = 250;

        private readonly IReviewerRepository _reviewerRepository;
        private readonly ISubmissionEvaluationRepository _evaluationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;

        public Handler(
            IReviewerRepository reviewerRepository,
            ISubmissionEvaluationRepository evaluationRepository,
            IApplicationLanguageProvider languageProvider)
        {
            _reviewerRepository = reviewerRepository;
            _evaluationRepository = evaluationRepository;
            _languageProvider = languageProvider;
        }

        public async Task<GetReviewerEvaluationListResponse> Handle(GetReviewerEvaluationListQuery request, CancellationToken cancellationToken)
        {
            if (!request.CurrentUserId.HasValue || request.CurrentUserId.Value == Guid.Empty)
                return new GetReviewerEvaluationListResponse();

            Reviewer? reviewer = await _reviewerRepository
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.UserId == request.CurrentUserId.Value &&
                    item.IsActive &&
                    item.DeletedDate == null,
                    cancellationToken);

            if (reviewer is null)
                return new GetReviewerEvaluationListResponse();

            Guid requestedLanguageId = await ResolveLanguageIdAsync(request.Culture, cancellationToken);
            Guid defaultLanguageId = await ResolveDefaultLanguageIdAsync(cancellationToken);

            IQueryable<SubmissionEvaluation> query = _evaluationRepository
                .Query()
                .AsNoTracking()
                .Include(item => item.Submission)
                    .ThenInclude(submission => submission.Congress)
                .Include(item => item.Submission)
                    .ThenInclude(submission => submission.SubmissionType)!
                        .ThenInclude(type => type.Translations)
                .Include(item => item.Submission)
                    .ThenInclude(submission => submission.Topic)!
                        .ThenInclude(topic => topic.Translations)
                .Include(item => item.Scores)
                .Where(item =>
                    item.ReviewerId == reviewer.Id &&
                    item.DeletedDate == null &&
                    item.Submission.DeletedDate == null &&
                    item.Submission.Congress.DeletedDate == null &&
                    item.Submission.Congress.Status == CongressStatus.Published);

            if (request.CongressId.HasValue && request.CongressId.Value != Guid.Empty)
                query = query.Where(item => item.Submission.CongressId == request.CongressId.Value);

            if (request.TopicId.HasValue && request.TopicId.Value != Guid.Empty)
                query = query.Where(item => item.Submission.TopicId == request.TopicId.Value);

            if (request.SubmissionTypeId.HasValue && request.SubmissionTypeId.Value != Guid.Empty)
                query = query.Where(item => item.Submission.SubmissionTypeId == request.SubmissionTypeId.Value);

            List<SubmissionEvaluation> evaluations = await query
                .OrderBy(item => item.CompletedAt.HasValue)
                .ThenBy(item => item.CompletedAt ?? item.UpdatedDate ?? item.CreatedDate)
                .ToListAsync(cancellationToken);

            DateTime today = DateTime.UtcNow.Date;

            List<GetReviewerEvaluationListItemDto> projectedItems = evaluations.Select(evaluation =>
            {
                DateTime dueDate = ResolveDueDate(evaluation);
                bool hasDraft = evaluation.Scores.Any(score => score.DeletedDate == null)
                    || !string.IsNullOrWhiteSpace(evaluation.Comment)
                    || !string.IsNullOrWhiteSpace(evaluation.EditorComment)
                    || !string.IsNullOrWhiteSpace(evaluation.Recommendation);
                bool isCompleted = evaluation.CompletedAt.HasValue;
                int daysRemaining = (int)Math.Ceiling((dueDate.Date - today).TotalDays);

                return new GetReviewerEvaluationListItemDto
                {
                    EvaluationId = evaluation.Id,
                    SubmissionId = evaluation.SubmissionId,
                    CongressId = evaluation.Submission.CongressId,
                    SubmissionTypeId = evaluation.Submission.SubmissionTypeId,
                    TopicId = evaluation.Submission.TopicId,
                    SubmissionNumber = string.IsNullOrWhiteSpace(evaluation.Submission.SubmissionNumber)
                        ? evaluation.Submission.Id.ToString("N")[..8].ToUpperInvariant()
                        : evaluation.Submission.SubmissionNumber,
                    Title = evaluation.Submission.Title,
                    TitleEn = evaluation.Submission.TitleEn,
                    SubmissionTypeName = ResolveSubmissionTypeName(evaluation, requestedLanguageId, defaultLanguageId),
                    TopicName = ResolveTopicName(evaluation, requestedLanguageId, defaultLanguageId),
                    CongressName = evaluation.Submission.Congress?.Name ?? "-",
                    AssignedDate = evaluation.CreatedDate,
                    DueDate = dueDate,
                    StatusText = ResolveStatusText(isCompleted),
                    StatusBadgeClass = ResolveStatusBadgeClass(isCompleted, daysRemaining),
                    RecommendationText = ResolveRecommendationText(evaluation.Recommendation),
                    TotalScore = evaluation.TotalScore,
                    CompletedAt = evaluation.CompletedAt,
                    HasDraft = hasDraft,
                    IsCompleted = isCompleted,
                    IsOverdue = !isCompleted && daysRemaining < 0,
                    IsDueSoon = !isCompleted && daysRemaining >= 0 && daysRemaining <= 2,
                    DaysRemaining = daysRemaining,
                    ActionText = ResolveActionText(isCompleted),
                    ActionIcon = isCompleted ? "ri-eye-line" : "ri-edit-line"
                };
            }).ToList();

            projectedItems = ApplySearch(projectedItems, request.SearchText).ToList();
            projectedItems = ApplyStatusFilter(projectedItems, request.Status).ToList();

            int totalCount = projectedItems.Count;
            projectedItems = ApplySort(projectedItems, request.SortColumn, request.SortDirection).ToList();

            int page = request.PageRequest?.Page ?? DefaultPage;
            int pageSize = request.PageRequest?.PageSize ?? DefaultPageSize;
            if (page < 0)
                page = DefaultPage;
            if (pageSize <= 0)
                pageSize = DefaultPageSize;

            List<GetReviewerEvaluationListItemDto> pageItems = projectedItems
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToList();

            return new GetReviewerEvaluationListResponse
            {
                Items = pageItems,
                Count = totalCount
            };
        }

        private async Task<Guid> ResolveLanguageIdAsync(string? culture, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(culture))
            {
                var language = await _languageProvider.GetByCultureAsync(culture, cancellationToken);
                if (language is not null)
                    return language.Id;
            }

            return await ResolveDefaultLanguageIdAsync(cancellationToken);
        }

        private async Task<Guid> ResolveDefaultLanguageIdAsync(CancellationToken cancellationToken)
        {
            var defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            return defaultLanguage.Id;
        }

        private static IEnumerable<GetReviewerEvaluationListItemDto> ApplySearch(IEnumerable<GetReviewerEvaluationListItemDto> items, string? searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return items;

            string normalized = searchText.Trim();

            return items.Where(item =>
                Contains(item.SubmissionNumber, normalized) ||
                Contains(item.Title, normalized) ||
                Contains(item.TitleEn, normalized) ||
                Contains(item.SubmissionTypeName, normalized) ||
                Contains(item.TopicName, normalized) ||
                Contains(item.CongressName, normalized));
        }

        private static IEnumerable<GetReviewerEvaluationListItemDto> ApplyStatusFilter(IEnumerable<GetReviewerEvaluationListItemDto> items, string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return items;

            return status.Trim() switch
            {
                "pending" => items.Where(item => !item.IsCompleted),
                "inProgress" => items.Where(item => false),
                "completed" => items.Where(item => item.IsCompleted),
                "dueSoon" => items.Where(item => item.IsDueSoon),
                "overdue" => items.Where(item => item.IsOverdue),
                _ => items
            };
        }

        private static IEnumerable<GetReviewerEvaluationListItemDto> ApplySort(IEnumerable<GetReviewerEvaluationListItemDto> items, string? sortColumn, string? sortDirection)
        {
            bool descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            string column = string.IsNullOrWhiteSpace(sortColumn) ? "dueDate" : sortColumn.Trim();

            IOrderedEnumerable<GetReviewerEvaluationListItemDto> ordered = column switch
            {
                "submissionNumber" => Sort(items, item => item.SubmissionNumber, descending),
                "type" => Sort(items, item => item.SubmissionTypeName, descending),
                "title" => Sort(items, item => item.Title, descending),
                "topic" => Sort(items, item => item.TopicName, descending),
                "congress" => Sort(items, item => item.CongressName, descending),
                "assignedDate" => Sort(items, item => item.AssignedDate, descending),
                "status" => Sort(items, item => item.StatusText, descending),
                "recommendation" => Sort(items, item => item.RecommendationText, descending),
                _ => Sort(items, item => item.DueDate, descending)
            };

            return ordered.ThenBy(item => item.IsCompleted).ThenBy(item => item.Title);
        }

        private static IOrderedEnumerable<GetReviewerEvaluationListItemDto> Sort<TKey>(IEnumerable<GetReviewerEvaluationListItemDto> items, Func<GetReviewerEvaluationListItemDto, TKey> selector, bool descending)
        {
            return descending ? items.OrderByDescending(selector) : items.OrderBy(selector);
        }

        private static bool Contains(string? value, string searchText)
        {
            return !string.IsNullOrWhiteSpace(value) && value.Contains(searchText, StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveSubmissionTypeName(SubmissionEvaluation evaluation, Guid requestedLanguageId, Guid defaultLanguageId)
        {
            var translations = evaluation.Submission.SubmissionType?.Translations?.Where(item => item.DeletedDate == null).ToList() ?? new();
            return translations.FirstOrDefault(item => item.LanguageId == requestedLanguageId)?.Name
                ?? translations.FirstOrDefault(item => item.LanguageId == defaultLanguageId)?.Name
                ?? evaluation.Submission.SubmissionType?.Code
                ?? "-";
        }

        private static string ResolveTopicName(SubmissionEvaluation evaluation, Guid requestedLanguageId, Guid defaultLanguageId)
        {
            var translations = evaluation.Submission.Topic?.Translations?.Where(item => item.DeletedDate == null).ToList() ?? new();
            return translations.FirstOrDefault(item => item.LanguageId == requestedLanguageId)?.Name
                ?? translations.FirstOrDefault(item => item.LanguageId == defaultLanguageId)?.Name
                ?? evaluation.Submission.Topic?.Code
                ?? "-";
        }

        private static DateTime ResolveDueDate(SubmissionEvaluation evaluation)
        {
            // Assignment due date field is not available in current schema; first phase uses seven days as default.
            return evaluation.CreatedDate == default
                ? DateTime.UtcNow.Date.AddDays(7)
                : evaluation.CreatedDate.Date.AddDays(7);
        }

        private static string ResolveStatusText(bool isCompleted)
        {
            return isCompleted
                ? ReviewerEvaluationResourceKeys.StatusCompleted
                : ReviewerEvaluationResourceKeys.StatusPending;
        }

        private static string ResolveActionText(bool isCompleted)
        {
            return isCompleted
                ? ReviewerEvaluationResourceKeys.ActionView
                : ReviewerEvaluationResourceKeys.ActionEvaluate;
        }

        private static string ResolveRecommendationText(string? recommendation)
        {
            if (string.IsNullOrWhiteSpace(recommendation))
                return ReviewerEvaluationResourceKeys.RecommendationNone;

            return recommendation.Trim() switch
            {
                "Kabul" => ReviewerEvaluationResourceKeys.RecommendationAccept,
                "Accept" => ReviewerEvaluationResourceKeys.RecommendationAccept,
                "Küçük Revizyon" => ReviewerEvaluationResourceKeys.RecommendationMinorRevision,
                "Minor Revision" => ReviewerEvaluationResourceKeys.RecommendationMinorRevision,
                "Büyük Revizyon" => ReviewerEvaluationResourceKeys.RecommendationMajorRevision,
                "Major Revision" => ReviewerEvaluationResourceKeys.RecommendationMajorRevision,
                "Ret" => ReviewerEvaluationResourceKeys.RecommendationReject,
                "Reject" => ReviewerEvaluationResourceKeys.RecommendationReject,
                _ => recommendation.Trim()
            };
        }

        private static string ResolveStatusBadgeClass(bool isCompleted, int daysRemaining)
        {
            if (isCompleted)
                return "bg-success-100 text-success-600";

            if (daysRemaining < 0)
                return "bg-danger-100 text-danger-600";

            return "bg-warning-100 text-warning-600";
        }
    }
}
