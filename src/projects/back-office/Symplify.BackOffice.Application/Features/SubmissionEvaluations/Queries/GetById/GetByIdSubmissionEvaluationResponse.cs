namespace Symplify.BackOffice.Application.Features.SubmissionEvaluations.Queries.GetById;
public class GetByIdSubmissionEvaluationResponse
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid ReviewerId { get; set; }
    public string? Comment { get; set; }
    public string? Recommendation { get; set; }
    public decimal? TotalScore { get; set; }
    public DateTime? CompletedAt { get; set; }
}
