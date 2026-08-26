using Symplify.BackOffice.Application.Features.ReviewerEvaluations.Constants;

namespace Symplify.BackOffice.Application.Features.ReviewerEvaluations.Queries.GetForm;

public sealed class GetReviewerEvaluationFormResponse
{
    public Guid EvaluationId { get; set; }
    public Guid SubmissionId { get; set; }
    public string SubmissionNumber { get; set; } = string.Empty;
    public string CongressName { get; set; } = "-";
    public string SubmissionTypeName { get; set; } = "-";
    public string TopicName { get; set; } = "-";
    public string Title { get; set; } = string.Empty;
    public string? TitleEn { get; set; }
    public string? Abstract { get; set; }
    public string? AbstractEn { get; set; }
    public string? Keywords { get; set; }
    public string? KeywordsEn { get; set; }
    public DateTime AssignedDate { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string StatusText { get; set; } = ReviewerEvaluationResourceKeys.StatusPending;
    public string StatusBadgeClass { get; set; } = "bg-warning-100 text-warning-600";
    public string? Recommendation { get; set; }
    public string? Comment { get; set; }
    public string? EditorComment { get; set; }
    public decimal? TotalScore { get; set; }
    public decimal MaxScore { get; set; }
    public List<ReviewerEvaluationScoreOptionDto> ScoreOptions { get; set; } = new();
    public List<ReviewerEvaluationCriterionDto> Criteria { get; set; } = new();
    public List<ReviewerEvaluationFileDto> Files { get; set; } = new();
}
