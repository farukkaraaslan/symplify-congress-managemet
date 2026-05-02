namespace Symplify.BackOffice.Application.Features.SubmissionHistories.Commands.Create;
public class CreatedSubmissionHistoryResponse
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public int? FromStatusId { get; set; }
    public int? ToStatusId { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public string? Note { get; set; }
}
