namespace Symplify.BackOffice.Application.Features.Submissions.Commands.Create;

public sealed class CreatedSubmissionResponse
{
    public Guid Id { get; set; }

    public Guid CongressId { get; set; }

    public Guid? SubmissionTypeId { get; set; }

    public Guid? TopicId { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public int? TransactionStatusId { get; set; }

    public string SubmissionNumber { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? TitleEn { get; set; }

    public string? Abstract { get; set; }

    public string? AbstractEn { get; set; }

    public string? Keywords { get; set; }

    public string? KeywordsEn { get; set; }

    public bool IsSubmitted { get; set; }

    public DateTime? SubmittedAt { get; set; }
}
