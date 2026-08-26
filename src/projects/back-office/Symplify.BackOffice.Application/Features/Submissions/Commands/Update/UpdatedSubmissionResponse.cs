namespace Symplify.BackOffice.Application.Features.Submissions.Commands.Update;
public class UpdatedSubmissionResponse
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public Guid? SubmissionTypeId { get; set; }
    public Guid? TopicId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? LanguageId { get; set; }
    public int? PaymentStatusId { get; set; }
    public int? TransactionStatusId { get; set; }
    public string SubmissionNumber { get; set; } = string.Empty;
    public string? Orcid { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TitleEn { get; set; }
    public string? Abstract { get; set; }
    public string? AbstractEn { get; set; }
    public string? Keywords { get; set; }
    public string? KeywordsEn { get; set; }
    public DateTime? SubmittedAt { get; set; }
}
