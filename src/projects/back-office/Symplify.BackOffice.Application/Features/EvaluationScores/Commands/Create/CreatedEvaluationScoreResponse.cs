namespace Symplify.BackOffice.Application.Features.EvaluationScores.Commands.Create;
public class CreatedEvaluationScoreResponse
{
    public Guid Id { get; set; }
    public Guid SubmissionEvaluationId { get; set; }
    public Guid EvaluationCriterionId { get; set; }
    public decimal Score { get; set; }
    public string? Comment { get; set; }
}
