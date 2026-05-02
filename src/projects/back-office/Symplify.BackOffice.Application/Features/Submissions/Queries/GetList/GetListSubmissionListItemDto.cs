namespace Symplify.BackOffice.Application.Features.Submissions.Queries.GetList;
public class GetListSubmissionListItemDto
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public Guid? SubmissionTypeId { get; set; }
    public Guid? TopicId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public int? PaymentStatusId { get; set; }
    public int? TransactionStatusId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Abstract { get; set; }
    public string? Keywords { get; set; }
}
