namespace Symplify.BackOffice.Application.Features.Submissions.Queries.GetList;

public sealed class GetListSubmissionListItemDto
{
    public Guid Id { get; set; }

    public Guid CongressId { get; set; }

    public string? CongressCode { get; set; }

    public string CongressName { get; set; } = "-";

    public Guid? SubmissionTypeId { get; set; }

    public Guid? TopicId { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public string? SubmissionOwnerName { get; set; }

    public string? SubmissionOwnerEmail { get; set; }

    public int OwnerSubmissionCount { get; set; }

    public bool HasMultipleSubmissions { get; set; }

    public Guid? LanguageId { get; set; }

    public int? PaymentStatusId { get; set; }

    public string? PaymentStatusCode { get; set; }

    public int? TransactionStatusId { get; set; }

    public string? TransactionStatusCode { get; set; }

    public string SubmissionNumber { get; set; } = string.Empty;

    public string? Orcid { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? TitleEn { get; set; }

    public string? Abstract { get; set; }

    public string? AbstractEn { get; set; }

    public string? Keywords { get; set; }

    public string? KeywordsEn { get; set; }

    public bool IsSubmitted { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string SubmissionTypeName { get; set; } = "-";

    public string TopicName { get; set; } = "-";

    public string PaymentStatusName { get; set; } = "-";

    public string TransactionStatusName { get; set; } = "Taslak";

    public string PaymentStatusBadgeClass { get; set; } = "bg-neutral-200 text-neutral-700";

    public string TransactionStatusBadgeClass { get; set; } = "bg-neutral-200 text-neutral-700";

    public string? CorrespondingAuthorName { get; set; }

    public string? OtherAuthorsText { get; set; }

    public int AuthorCount { get; set; }

    public bool CanEdit { get; set; }

    public bool CanDelete { get; set; }
}
