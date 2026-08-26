namespace Symplify.BackOffice.Application.Features.EvaluationScores.Queries.GetById;
public class GetByIdEvaluationScoreResponse
{
    public Guid Id { get; set; }
    public Guid SubmissionEvaluationId { get; set; }
    public Guid EvaluationCriterionId { get; set; }
    public decimal Score { get; set; }
    public string? Comment { get; set; }
}
