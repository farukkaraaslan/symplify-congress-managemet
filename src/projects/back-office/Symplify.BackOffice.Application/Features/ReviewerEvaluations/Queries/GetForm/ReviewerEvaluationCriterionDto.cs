namespace Symplify.BackOffice.Application.Features.ReviewerEvaluations.Queries.GetForm;

public sealed class ReviewerEvaluationCriterionDto
{
    public Guid EvaluationCriterionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public int MaxScore { get; set; } = 10;
    public decimal? Score { get; set; }
    public string? Comment { get; set; }
}
