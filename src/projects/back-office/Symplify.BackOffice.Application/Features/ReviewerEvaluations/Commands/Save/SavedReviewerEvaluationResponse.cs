namespace Symplify.BackOffice.Application.Features.ReviewerEvaluations.Commands.Save;

public sealed class SavedReviewerEvaluationResponse
{
    public Guid EvaluationId { get; set; }
    public Guid SubmissionId { get; set; }
    public bool IsCompleted { get; set; }
    public decimal? TotalScore { get; set; }
}
