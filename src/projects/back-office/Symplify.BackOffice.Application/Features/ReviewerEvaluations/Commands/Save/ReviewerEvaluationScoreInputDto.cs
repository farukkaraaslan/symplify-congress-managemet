namespace Symplify.BackOffice.Application.Features.ReviewerEvaluations.Commands.Save;

public sealed class ReviewerEvaluationScoreInputDto
{
    public Guid EvaluationCriterionId { get; set; }
    public decimal? Score { get; set; }
    public string? Comment { get; set; }
}
