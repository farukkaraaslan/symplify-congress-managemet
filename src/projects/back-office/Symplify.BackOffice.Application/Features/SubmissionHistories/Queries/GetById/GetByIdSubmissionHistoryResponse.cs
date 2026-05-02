namespace Symplify.BackOffice.Application.Features.SubmissionHistories.Queries.GetById;
public class GetByIdSubmissionHistoryResponse
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public int? FromStatusId { get; set; }
    public int? ToStatusId { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public string? Note { get; set; }
}
