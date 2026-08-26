namespace Symplify.BackOffice.Application.Features.ParticipationCertificates.Services;

public interface IParticipationCertificateService
{
    Task<IReadOnlyList<ParticipationCertificateCongressOptionDto>> GetCongressOptionsAsync(
        string? culture,
        Guid? includeCongressId = null,
        CancellationToken cancellationToken = default);

    Task<ParticipationCertificateDashboardDto> GetDashboardAsync(
        Guid congressId,
        string? culture,
        ParticipationCertificateDashboardFilter? filter = null,
        CancellationToken cancellationToken = default);

    Task<ParticipationCertificateCandidatePageResult> GetCandidatePageAsync(
        ParticipationCertificateCandidatePageRequest request,
        CancellationToken cancellationToken = default);

    Task<ParticipationCertificateDocumentPageResult> GetDocumentPageAsync(
        ParticipationCertificateDocumentPageRequest request,
        CancellationToken cancellationToken = default);

    Task<ParticipationCertificateStoredFileDto?> GetGeneratedFileAsync(
        Guid certificateId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParticipationCertificateStoredFileDto>> GetGeneratedFilesAsync(
        Guid congressId,
        IReadOnlyCollection<Guid>? certificateIds = null,
        CancellationToken cancellationToken = default);

    Task<ParticipationCertificateTemplateDto> UploadTemplateAsync(
        ParticipationCertificateTemplateUploadInput input,
        CancellationToken cancellationToken = default);

    Task SetDefaultTemplateAsync(
        Guid congressId,
        string? culture,
        Guid? performedByUserId,
        CancellationToken cancellationToken = default);

    Task SaveTemplateSettingsAsync(
        Guid congressId,
        string? culture,
        string? bodyText,
        string? nameFontColorHex,
        string? mailSubject,
        string? mailTitle,
        string? mailBodyHtml,
        bool isDefault,
        Guid? performedByUserId,
        CancellationToken cancellationToken = default);

    Task<ParticipationCertificateOperationResult> GenerateAsync(
        Guid congressId,
        string? certificateCulture,
        Guid? performedByUserId,
        CancellationToken cancellationToken = default);

    Task<ParticipationCertificateGenerationJobDto> QueueGenerationAsync(
        ParticipationCertificateGenerationQueueInput input,
        CancellationToken cancellationToken = default);

    Task<ParticipationCertificateGenerationJobDto?> GetGenerationJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default);

    Task<ParticipationCertificateGenerationJobDto?> GetLatestGenerationJobAsync(
        Guid congressId,
        string? culture,
        CancellationToken cancellationToken = default);

    Task CancelGenerationJobAsync(
        Guid jobId,
        Guid? performedByUserId,
        CancellationToken cancellationToken = default);

    Task<bool> ProcessNextGenerationJobAsync(CancellationToken cancellationToken = default);

    Task<ParticipationCertificateOperationResult> QueueEmailsAsync(
        Guid congressId,
        IReadOnlyCollection<Guid> certificateIds,
        Guid? performedByUserId,
        CancellationToken cancellationToken = default);

    Task<ParticipationCertificateOperationResult> RequestEmailQueueAsync(
        ParticipationCertificateEmailQueueInput input,
        CancellationToken cancellationToken = default);

    Task<int> ProcessRequestedEmailQueueBatchAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default);

    Task<ParticipationCertificatePublicAccessResult> ResolvePublicAccessAsync(
        Guid publicId,
        string token,
        CancellationToken cancellationToken = default);

    Task<ParticipationCertificateRevokeResult> RevokeAsync(
        Guid certificateId,
        string? reason,
        Guid? performedByUserId,
        CancellationToken cancellationToken = default);
}
