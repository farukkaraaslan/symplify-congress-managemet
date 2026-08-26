namespace Symplify.BackOffice.Application.Features.ParticipationCertificates.Services;

public sealed class ParticipationCertificateTemplateUploadInput
{
    public Guid CongressId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = "application/pdf";
    public long Length { get; init; }
    public Stream Content { get; init; } = Stream.Null;
    public string Culture { get; init; } = ParticipationCertificateCultures.Turkish;
    public bool IsDefault { get; init; }
    public string? NameFontColorHex { get; init; }
    public string? BodyText { get; init; }
    public string? MailSubject { get; init; }
    public string? MailTitle { get; init; }
    public string? MailBodyHtml { get; init; }
    public bool RenderCommitteeSignature { get; init; } = true;
}

public sealed class ParticipationCertificateDashboardFilter
{
    public string? SubmissionStatusCode { get; init; }
    public string? PaymentStatusCode { get; init; }
    public string? CertificateCulture { get; init; }
    public string? CandidateSearch { get; init; }
    public int CandidatePage { get; init; } = 1;
    public int CandidatePageSize { get; init; } = 100;
}

public sealed class ParticipationCertificateCandidatePageRequest
{
    public Guid CongressId { get; init; }
    public string? DisplayCulture { get; init; }
    public string? SubmissionStatusCode { get; init; }
    public string? PaymentStatusCode { get; init; }
    public string? SearchText { get; init; }
    public int Start { get; init; }
    public int Length { get; init; } = 25;
    public string? SortColumn { get; init; }
    public string? SortDirection { get; init; }
}

public sealed class ParticipationCertificateCandidatePageResult
{
    public int TotalCount { get; init; }
    public int FilteredCount { get; init; }
    public IReadOnlyList<ParticipationCertificateSubmissionCandidateDto> Items { get; init; } = Array.Empty<ParticipationCertificateSubmissionCandidateDto>();
}

public sealed class ParticipationCertificateDocumentPageRequest
{
    public Guid CongressId { get; init; }
    public string? CertificateCulture { get; init; }
    public string? EmailStatus { get; init; }
    public bool IncludeRevoked { get; init; }
    public string? SearchText { get; init; }
    public int Start { get; init; }
    public int Length { get; init; } = 25;
    public string? SortColumn { get; init; }
    public string? SortDirection { get; init; }
}

public sealed class ParticipationCertificateDocumentPageResult
{
    public int TotalCount { get; init; }
    public int FilteredCount { get; init; }
    public IReadOnlyList<ParticipationCertificateDocumentDto> Items { get; init; } = Array.Empty<ParticipationCertificateDocumentDto>();
}

public sealed class ParticipationCertificateGenerationQueueInput
{
    public Guid CongressId { get; init; }
    public string? CertificateCulture { get; init; }
    public string? SubmissionStatusCode { get; init; }
    public string? PaymentStatusCode { get; init; }
    public string? CandidateSearch { get; init; }
    public bool SelectAllFiltered { get; init; }
    public IReadOnlyCollection<string> SelectedCandidateKeys { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> ExcludedCandidateKeys { get; init; } = Array.Empty<string>();
    public Guid? RequestedByUserId { get; init; }
}

public sealed class ParticipationCertificateEmailQueueInput
{
    public Guid CongressId { get; init; }
    public string? CertificateCulture { get; init; }
    public string? EmailStatus { get; init; }
    public string? CandidateSearch { get; init; }
    public bool SelectAllFiltered { get; init; }
    public IReadOnlyCollection<Guid> CertificateIds { get; init; } = Array.Empty<Guid>();
    public IReadOnlyCollection<Guid> ExcludedCertificateIds { get; init; } = Array.Empty<Guid>();
    public Guid? RequestedByUserId { get; init; }
}

public sealed class ParticipationCertificateGenerationJobDto
{
    public Guid Id { get; init; }
    public Guid CongressId { get; init; }
    public string Culture { get; init; } = ParticipationCertificateCultures.Turkish;
    public string Status { get; init; } = "Pending";
    public int TotalCount { get; init; }
    public int ProcessedCount { get; init; }
    public int SucceededCount { get; init; }
    public int FailedCount { get; init; }
    public int SkippedCount { get; init; }
    public int ExcludedCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? LastError { get; init; }
    public bool IsActive { get; init; }
    public int ProgressPercent => TotalCount <= 0
        ? (IsActive ? 0 : 100)
        : Math.Clamp((int)Math.Round(ProcessedCount * 100d / TotalCount), 0, 100);
}

public sealed class ParticipationCertificateCongressOptionDto
{
    public Guid Id { get; init; }
    public string Text { get; init; } = string.Empty;
}

public sealed class ParticipationCertificateFilterOptionDto
{
    public string Code { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed class ParticipationCertificateTemplateDto
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public DateTime UploadedAt { get; init; }
    public string Culture { get; init; } = ParticipationCertificateCultures.Turkish;
    public string CultureDisplayName => ParticipationCertificateCultures.GetDisplayName(Culture);
    public bool IsDefault { get; init; }
    public string? BodyText { get; init; }
    public bool HasBodyText => !string.IsNullOrWhiteSpace(BodyText);
    public string? MailSubject { get; init; }
    public string? MailTitle { get; init; }
    public string? MailBodyHtml { get; init; }
    public bool HasMailTemplate =>
        !string.IsNullOrWhiteSpace(MailSubject) &&
        !string.IsNullOrWhiteSpace(MailBodyHtml) &&
        MailBodyHtml.Contains("{{CERTIFICATE_LINK}}", StringComparison.OrdinalIgnoreCase);
    public float NameBoxX { get; init; }
    public float NameBoxY { get; init; }
    public float NameBoxWidth { get; init; }
    public float NameBoxHeight { get; init; }
    public float NameFontSize { get; init; }
    public string NameFontColorHex { get; init; } = "#0F3791";
    public bool CoverPlaceholderBackground { get; init; }
    public bool RenderCommitteeSignature { get; init; }
    public float CommitteeSignatureBoxX { get; init; }
    public float CommitteeSignatureBoxY { get; init; }
    public float CommitteeSignatureBoxWidth { get; init; }
    public float CommitteeSignatureBoxHeight { get; init; }
}

public sealed class ParticipationCertificateCandidateDto
{
    public Guid SubmissionId { get; init; }
    public Guid AuthorId { get; init; }
    public string GenerationKey => $"{SubmissionId:N}:{AuthorId:N}";
    public string SubmissionNumber { get; init; } = string.Empty;
    public string SubmissionTitle { get; init; } = string.Empty;
    public string SubmissionTypeName { get; init; } = string.Empty;
    public string AuthorFullName { get; init; } = string.Empty;
    public string AuthorDisplayNameWithTitle { get; init; } = string.Empty;
    public string? AcademicTitle { get; init; }
    public string? AuthorEmail { get; init; }
    public string? AuthorInstitution { get; init; }
    public string? SubmissionStatusCode { get; init; }
    public string SubmissionStatusName { get; init; } = string.Empty;
    public string? PaymentStatusCode { get; init; }
    public string PaymentStatusName { get; init; } = string.Empty;
    public bool IsEligible { get; init; }
    public bool IsVideoPresentation { get; init; }
    public Guid? TurkishCertificateId { get; init; }
    public Guid? EnglishCertificateId { get; init; }
    public bool HasTurkishCertificate => TurkishCertificateId.HasValue;
    public bool HasEnglishCertificate => EnglishCertificateId.HasValue;
}

public sealed class ParticipationCertificateSubmissionCandidateDto
{
    public Guid SubmissionId { get; init; }
    public string GenerationKey => SubmissionId.ToString("N");
    public string SubmissionNumber { get; init; } = string.Empty;
    public string SubmissionTitle { get; init; } = string.Empty;
    public string SubmissionTypeName { get; init; } = string.Empty;
    public string? SubmissionStatusCode { get; init; }
    public string SubmissionStatusName { get; init; } = string.Empty;
    public string? PaymentStatusCode { get; init; }
    public string PaymentStatusName { get; init; } = string.Empty;
    public bool IsEligible { get; init; }
    public bool IsVideoPresentation { get; init; }
    public int AuthorCount { get; init; }
    public int EligibleAuthorCount { get; init; }
    public string AuthorNames { get; init; } = string.Empty;
    public string? AuthorEmails { get; init; }
    public string? Institutions { get; init; }
    public int TurkishCertificateCount { get; init; }
    public int EnglishCertificateCount { get; init; }
    public bool HasAllTurkishCertificates => AuthorCount > 0 && TurkishCertificateCount >= AuthorCount;
    public bool HasAllEnglishCertificates => AuthorCount > 0 && EnglishCertificateCount >= AuthorCount;
}

public sealed class ParticipationCertificateDocumentDto
{
    public Guid Id { get; init; }
    public Guid SubmissionId { get; init; }
    public Guid AuthorId { get; init; }
    public string SubmissionNumber { get; init; } = string.Empty;
    public string SubmissionTitle { get; init; } = string.Empty;
    public string AuthorFullName { get; init; } = string.Empty;
    public string? AuthorEmail { get; init; }
    public string Culture { get; init; } = ParticipationCertificateCultures.Turkish;
    public string CultureDisplayName => ParticipationCertificateCultures.GetDisplayName(Culture);
    public string FileName { get; init; } = string.Empty;
    public DateTime GeneratedAt { get; init; }
    public DateTime? EmailQueuedAt { get; init; }
    public DateTime? EmailSentAt { get; init; }
    public string? EmailStatus { get; init; }
    public DateTime? PublishedAt { get; init; }
    public DateTime? RevokedAt { get; init; }
    public string? RevocationReason { get; init; }
    public bool IsPublished => PublishedAt.HasValue && !RevokedAt.HasValue;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool CanQueueEmail =>
        !IsRevoked &&
        !string.IsNullOrWhiteSpace(AuthorEmail) &&
        !EmailSentAt.HasValue &&
        !string.Equals(EmailStatus, "QueueRequested", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(EmailStatus, "QueuePreparing", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(EmailStatus, "Queued", StringComparison.OrdinalIgnoreCase);
}

public sealed class ParticipationCertificateStoredFileDto
{
    public Guid Id { get; init; }
    public Guid CongressId { get; init; }
    public string SubmissionNumber { get; init; } = string.Empty;
    public string AuthorFullName { get; init; } = string.Empty;
    public string Culture { get; init; } = ParticipationCertificateCultures.Turkish;
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = "application/pdf";
    public string BucketName { get; init; } = string.Empty;
    public string ObjectName { get; init; } = string.Empty;
}

public enum ParticipationCertificatePublicAccessStatus
{
    NotFound = 0,
    InvalidToken = 1,
    NotPublished = 2,
    Revoked = 3,
    Available = 4
}

public sealed class ParticipationCertificatePublicAccessResult
{
    public ParticipationCertificatePublicAccessStatus Status { get; init; }
    public ParticipationCertificateStoredFileDto? File { get; init; }
    public string? Message { get; init; }
}

public sealed class ParticipationCertificateRevokeResult
{
    public Guid CertificateId { get; init; }
    public string? BucketName { get; init; }
    public string? ObjectName { get; init; }
    public bool AlreadyRevoked { get; init; }
    public bool StorageDeleteSucceeded { get; init; } = true;
}

public sealed class ParticipationCertificateDashboardDto
{
    public Guid CongressId { get; init; }
    public string CongressTitle { get; init; } = string.Empty;
    public ParticipationCertificateDashboardFilter Filter { get; init; } = new();
    public string CertificateCulture { get; init; } = ParticipationCertificateCultures.Turkish;
    public string DefaultCertificateCulture { get; init; } = ParticipationCertificateCultures.Turkish;
    public ParticipationCertificateTemplateDto? Template { get; init; }
    public IReadOnlyList<ParticipationCertificateTemplateDto> Templates { get; init; } = Array.Empty<ParticipationCertificateTemplateDto>();
    public IReadOnlyList<ParticipationCertificateFilterOptionDto> SubmissionStatusOptions { get; init; } = Array.Empty<ParticipationCertificateFilterOptionDto>();
    public IReadOnlyList<ParticipationCertificateFilterOptionDto> PaymentStatusOptions { get; init; } = Array.Empty<ParticipationCertificateFilterOptionDto>();
    public IReadOnlyList<ParticipationCertificateCandidateDto> Candidates { get; init; } = Array.Empty<ParticipationCertificateCandidateDto>();
    public int CandidateCount { get; init; }
    public int EligibleCandidateCount { get; init; }
    public int GeneratedCount { get; init; }
    public int EmailQueuedCount { get; init; }
    public int EmailSentCount { get; init; }
    public int RevokedCount { get; init; }
    public int MissingEmailCount { get; init; }
    public int MailSelectableCount { get; init; }
    public int CandidatePage { get; init; } = 1;
    public int CandidatePageSize { get; init; } = 100;
    public int CandidateTotalPages { get; init; } = 1;
    public string? CandidateSearch { get; init; }
    public ParticipationCertificateGenerationJobDto? GenerationJob { get; init; }
}

public sealed class ParticipationCertificateOperationResult
{
    public Guid? JobId { get; init; }
    public int CandidateCount { get; init; }
    public int CreatedOrUpdatedCount { get; init; }
    public int EmailQueuedCount { get; init; }
    public int SkippedCount { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

public sealed class ParticipationCertificatePdfRenderRequest
{
    public byte[] TemplatePdfBytes { get; init; } = Array.Empty<byte>();
    public string AuthorFullName { get; init; } = string.Empty;
    public string SubmissionTypeName { get; init; } = string.Empty;
    public string CertificateText { get; init; } = string.Empty;
    public float NameBoxX { get; init; }
    public float NameBoxY { get; init; }
    public float NameBoxWidth { get; init; }
    public float NameBoxHeight { get; init; }
    public float NameFontSize { get; init; }
    public string NameFontColorHex { get; init; } = "#FFFFFF";
    public bool CoverPlaceholderBackground { get; init; }
    public string PlaceholderBackgroundColorHex { get; init; } = "#06142E";
    public byte[]? CommitteeSignatureImageBytes { get; init; }
    public bool RenderCommitteeSignature { get; init; }
    public float CommitteeSignatureBoxX { get; init; }
    public float CommitteeSignatureBoxY { get; init; }
    public float CommitteeSignatureBoxWidth { get; init; }
    public float CommitteeSignatureBoxHeight { get; init; }
    public string CommitteeSignerFullName { get; init; } = string.Empty;
    public string? CommitteeSignerAcademicTitle { get; init; }
    public string? CommitteeSignerRole { get; init; }
}
