namespace Symplify.BackOffice.Application.Features.EvaluationScores.Commands.Delete;
public class DeletedEvaluationScoreResponse
{
    public Guid Id { get; set; }
    public Guid SubmissionEvaluationId { get; set; }
    public Guid EvaluationCriterionId { get; set; }
    public decimal Score { get; set; }
    public string? Comment { get; set; }
}
