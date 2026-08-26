using Symplify.BackOffice.Application.Features.ReviewerEvaluations.Constants;

namespace Symplify.BackOffice.Application.Features.ReviewerEvaluations.Queries.GetList;

public sealed class GetReviewerEvaluationListItemDto
{
    public Guid EvaluationId { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid CongressId { get; set; }
    public Guid? SubmissionTypeId { get; set; }
    public Guid? TopicId { get; set; }
    public string SubmissionNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? TitleEn { get; set; }
    public string SubmissionTypeName { get; set; } = "-";
    public string TopicName { get; set; } = "-";
    public string CongressName { get; set; } = "-";
    public DateTime AssignedDate { get; set; }
    public DateTime DueDate { get; set; }
    public string StatusText { get; set; } = ReviewerEvaluationResourceKeys.StatusPending;
    public string StatusBadgeClass { get; set; } = "bg-warning-100 text-warning-600";
    public string RecommendationText { get; set; } = ReviewerEvaluationResourceKeys.RecommendationNone;
    public decimal? TotalScore { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool HasDraft { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsOverdue { get; set; }
    public bool IsDueSoon { get; set; }
    public int DaysRemaining { get; set; }
    public string ActionText { get; set; } = ReviewerEvaluationResourceKeys.ActionEvaluate;
    public string ActionIcon { get; set; } = "ri-edit-line";
}
