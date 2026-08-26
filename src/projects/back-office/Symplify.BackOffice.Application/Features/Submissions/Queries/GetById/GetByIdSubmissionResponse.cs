using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.Submissions.Queries.GetById;

public sealed class GetByIdSubmissionResponse
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public Guid? SubmissionTypeId { get; set; }
    public SubmissionFormProfile FormProfile { get; set; } = SubmissionFormProfile.AcademicAbstract;
    public bool IsExhibitionApplication => FormProfile == SubmissionFormProfile.ExhibitionApplication;
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

    public bool IsSubmitted { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public string CongressName { get; set; } = "-";
    public string SubmissionTypeName { get; set; } = "-";
    public string TopicName { get; set; } = "-";
    public string LanguageName { get; set; } = "-";
    public string PaymentStatusName { get; set; } = "-";
    public string PaymentStatusCode { get; set; } = string.Empty;
    public string TransactionStatusName { get; set; } = string.Empty;
    public string TransactionStatusCode { get; set; } = string.Empty;
    public string PaymentStatusBadgeClass { get; set; } = "bg-neutral-200 text-neutral-700";
    public string TransactionStatusBadgeClass { get; set; } = "bg-warning-100 text-warning-600";

    public bool CanEdit { get; set; }
    public bool HasAuthorAction { get; set; }
    public bool IsDecisionCompleted { get; set; }
    public string AuthorActionTitle { get; set; } = string.Empty;
    public string AuthorActionDescription { get; set; } = string.Empty;
    public DateTime? AuthorActionDueDate { get; set; }

    public string? CorrespondingAuthorName { get; set; }
    public int CompletedEvaluationCount { get; set; }
    public int ReviewerCount { get; set; }
    public int FileCount { get; set; }
    public string? LatestFileName { get; set; }
    public bool CanUploadPaymentDocument { get; set; }

    public SubmissionDetailExhibitionDto? ExhibitionDetail { get; set; }

    public IReadOnlyList<SubmissionDetailAuthorDto> Authors { get; set; } = Array.Empty<SubmissionDetailAuthorDto>();
    public IReadOnlyList<SubmissionDetailReviewDto> Reviews { get; set; } = Array.Empty<SubmissionDetailReviewDto>();
    public IReadOnlyList<SubmissionDetailFileDto> Files { get; set; } = Array.Empty<SubmissionDetailFileDto>();
    public IReadOnlyList<SubmissionDetailPaymentDocumentDto> PaymentDocuments { get; set; } = Array.Empty<SubmissionDetailPaymentDocumentDto>();
    public IReadOnlyList<SubmissionDetailHistoryDto> Histories { get; set; } = Array.Empty<SubmissionDetailHistoryDto>();
}

public sealed class SubmissionDetailExhibitionDto
{
    public string WorkName { get; set; } = string.Empty;
    public string? Dimensions { get; set; }
    public string Technique { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Address { get; set; } = string.Empty;
}

public sealed class SubmissionDetailAuthorDto
{
    public Guid Id { get; set; }
    public Guid? TitleId { get; set; }
    public string? TitleName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Institution { get; set; }
    public string? Orcid { get; set; }
    public bool IsCorrespondingAuthor { get; set; }
}

public sealed class SubmissionDetailReviewDto
{
    public Guid Id { get; set; }
    public int Sequence { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public decimal? TotalScore { get; set; }
    public int ScoreCount { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedDate { get; set; }
    public IReadOnlyList<SubmissionDetailReviewScoreDto> Scores { get; set; } = Array.Empty<SubmissionDetailReviewScoreDto>();
}

public sealed class SubmissionDetailReviewScoreDto
{
    public Guid Id { get; set; }
    public string CriterionName { get; set; } = "-";
    public decimal Score { get; set; }
    public string? Comment { get; set; }
}

public sealed class SubmissionDetailFileDto
{
    public Guid Id { get; set; }
    public SubmissionFileKind FileKind { get; set; }
    public string FileKindText { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long? FileSize { get; set; }
    public bool IsActive { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? DisplayDate { get; set; }
    public bool DownloadByAcceptanceLetter { get; set; }
}

public sealed class SubmissionDetailPaymentDocumentDto
{
    public Guid Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long? Size { get; set; }
    public bool IsApproved { get; set; }
    public DateTime UploadedAt { get; set; }
}

public sealed class SubmissionDetailHistoryDto
{
    public Guid Id { get; set; }
    public string FromStatusName { get; set; } = "-";
    public string ToStatusName { get; set; } = "-";
    public string FromStatusCode { get; set; } = string.Empty;
    public string ToStatusCode { get; set; } = string.Empty;
    public string? DisplayTitle { get; set; }
    public string? DisplayDescription { get; set; }
    public string? SourceAction { get; set; }
    public string? PublicNote { get; set; }
    public string? Note { get; set; }
    public string PerformedByName { get; set; } = "-";
    public DateTime PerformedAt { get; set; }
    public bool IsAutomatic { get; set; }
}
