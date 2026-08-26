namespace Symplify.BackOffice.Application.Features.ReviewerEvaluations.Queries.GetForm;

public sealed class ReviewerEvaluationScoreOptionDto
{
    public decimal Value { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Order { get; set; }
}
