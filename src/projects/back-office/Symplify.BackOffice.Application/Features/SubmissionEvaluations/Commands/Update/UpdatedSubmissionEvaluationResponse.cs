namespace Symplify.BackOffice.Application.Features.SubmissionEvaluations.Commands.Update;
public class UpdatedSubmissionEvaluationResponse
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid ReviewerId { get; set; }
    public string? Comment { get; set; }
    public string? Recommendation { get; set; }
    public decimal? TotalScore { get; set; }
    public DateTime? CompletedAt { get; set; }
}
