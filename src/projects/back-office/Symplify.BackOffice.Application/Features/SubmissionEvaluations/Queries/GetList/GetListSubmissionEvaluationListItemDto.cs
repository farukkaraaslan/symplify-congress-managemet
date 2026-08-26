namespace Symplify.BackOffice.Application.Features.SubmissionEvaluations.Queries.GetList;
public class GetListSubmissionEvaluationListItemDto
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid ReviewerId { get; set; }
    public string? Comment { get; set; }
    public string? Recommendation { get; set; }
    public decimal? TotalScore { get; set; }
    public DateTime? CompletedAt { get; set; }
}
