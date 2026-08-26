namespace Symplify.BackOffice.Application.Features.Submissions.Commands.SaveEditorEvaluation;

public sealed class SavedEditorEvaluationResponse
{
    public Guid EvaluationId { get; set; }
    public Guid SubmissionId { get; set; }
    public bool IsCompleted { get; set; }
    public decimal? TotalScore { get; set; }
}
