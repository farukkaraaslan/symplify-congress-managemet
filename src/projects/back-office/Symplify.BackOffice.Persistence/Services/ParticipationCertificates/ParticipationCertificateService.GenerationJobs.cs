using System.Text.Json;
using Core.Application.Storage;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Common.Storage;
using Symplify.BackOffice.Application.Features.ParticipationCertificates.Services;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Persistence.Services.ParticipationCertificates;

public sealed partial class ParticipationCertificateService
{
    private const int GenerationBatchSize = 25;
    private const int MaterializationBatchSize = 500;
    private const int MaxExcludedCandidateCount = 50000;

    public async Task<ParticipationCertificateGenerationJobDto> QueueGenerationAsync(
        ParticipationCertificateGenerationQueueInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.CongressId == Guid.Empty)
            throw new InvalidOperationException("Kongre bilgisi geçersiz.");

        string culture = ParticipationCertificateCultures.Normalize(input.CertificateCulture);
        IReadOnlyList<ParticipationCertificateTemplate> templates = await GetActiveTemplatesAsync(input.CongressId, cancellationToken);
        ParticipationCertificateTemplate template = templates.FirstOrDefault(item =>
                string.Equals(item.Culture, culture, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"{ParticipationCertificateCultures.GetDisplayName(culture)} katılım sertifikası için aktif template PDF dosyası yok.");

        if (string.IsNullOrWhiteSpace(NormalizeCertificateBodyText(template.BodyText)))
            throw new InvalidOperationException(
                $"{ParticipationCertificateCultures.GetDisplayName(culture)} sertifika metni girilmemiş. Arayüzden sertifika metnini kaydetmeden belge oluşturamazsınız.");

        string generationMailSubject = NormalizeMailSubject(template.MailSubject);
        string generationMailBodyHtml = NormalizeMailBodyHtml(template.MailBodyHtml);
        try
        {
            ValidateMailTemplate(generationMailSubject, generationMailBodyHtml);
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidOperationException(
                $"{ParticipationCertificateCultures.GetDisplayName(culture)} mail template ayarları tamamlanmadan belge oluşturamazsınız. {exception.Message}",
                exception);
        }

        ParticipationCertificateGenerationJob? activeJob = await _context.ParticipationCertificateGenerationJobs
            .AsNoTracking()
            .Where(job =>
                job.CongressId == input.CongressId &&
                job.Culture == culture &&
                (job.Status == ParticipationCertificateGenerationJobStatus.Pending ||
                 job.Status == ParticipationCertificateGenerationJobStatus.Preparing ||
                 job.Status == ParticipationCertificateGenerationJobStatus.Processing ||
                 job.Status == ParticipationCertificateGenerationJobStatus.CancelRequested))
            .OrderByDescending(job => job.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeJob is not null)
        {
            throw new InvalidOperationException(
                $"{ParticipationCertificateCultures.GetDisplayName(culture)} için halen çalışan bir belge üretim işi var. " +
                "Mevcut iş tamamlandıktan veya iptal edildikten sonra yeni seçimi başlatın.");
        }

        IReadOnlyList<string> selectedKeys = NormalizeCandidateKeys(input.SelectedCandidateKeys);
        IReadOnlyList<string> excludedKeys = NormalizeCandidateKeys(input.ExcludedCandidateKeys);

        if (!input.SelectAllFiltered && selectedKeys.Count == 0)
            throw new InvalidOperationException("Belge oluşturmak için en az bir bildiri seçmelisiniz.");

        DateTime now = DateTime.UtcNow;
        string actor = input.RequestedByUserId?.ToString("D") ?? "ParticipationCertificateGenerationQueued";

        ParticipationCertificateGenerationJob job = new()
        {
            Id = Guid.NewGuid(),
            CongressId = input.CongressId,
            Culture = culture,
            SubmissionStatusCode = NormalizeFilterCode(input.SubmissionStatusCode),
            PaymentStatusCode = NormalizeFilterCode(input.PaymentStatusCode),
            CandidateSearch = string.IsNullOrWhiteSpace(input.CandidateSearch) ? null : input.CandidateSearch.Trim(),
            SelectAllFiltered = input.SelectAllFiltered,
            SelectedCandidateKeysJson = JsonSerializer.Serialize(selectedKeys),
            ExcludedCandidateKeysJson = JsonSerializer.Serialize(excludedKeys),
            ExcludedCount = excludedKeys.Count,
            Status = ParticipationCertificateGenerationJobStatus.Pending,
            RequestedByUserId = input.RequestedByUserId,
            CreatedDate = now,
            CreatedBy = actor
        };

        _context.ParticipationCertificateGenerationJobs.Add(job);
        await _context.SaveChangesAsync(cancellationToken);
        return MapGenerationJob(job);
    }

    public async Task<ParticipationCertificateGenerationJobDto?> GetGenerationJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        if (jobId == Guid.Empty)
            return null;

        ParticipationCertificateGenerationJob? job = await _context.ParticipationCertificateGenerationJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == jobId, cancellationToken);
        return job is null ? null : MapGenerationJob(job);
    }

    public async Task<ParticipationCertificateGenerationJobDto?> GetLatestGenerationJobAsync(
        Guid congressId,
        string? culture,
        CancellationToken cancellationToken = default)
    {
        if (congressId == Guid.Empty)
            return null;

        string normalizedCulture = ParticipationCertificateCultures.Normalize(culture);
        ParticipationCertificateGenerationJob? job = await _context.ParticipationCertificateGenerationJobs
            .AsNoTracking()
            .Where(item => item.CongressId == congressId && item.Culture == normalizedCulture)
            .OrderByDescending(item => item.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);
        return job is null ? null : MapGenerationJob(job);
    }

    public async Task CancelGenerationJobAsync(
        Guid jobId,
        Guid? performedByUserId,
        CancellationToken cancellationToken = default)
    {
        ParticipationCertificateGenerationJob job = await _context.ParticipationCertificateGenerationJobs
            .FirstOrDefaultAsync(item => item.Id == jobId, cancellationToken)
            ?? throw new InvalidOperationException("Belge üretim işi bulunamadı.");

        if (!IsActiveGenerationStatus(job.Status))
            return;

        job.Status = ParticipationCertificateGenerationJobStatus.CancelRequested;
        job.UpdatedDate = DateTime.UtcNow;
        job.UpdatedBy = performedByUserId?.ToString("D") ?? "ParticipationCertificateGenerationCancelRequested";
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ProcessNextGenerationJobAsync(CancellationToken cancellationToken = default)
    {
        ParticipationCertificateGenerationJob? job = await _context.ParticipationCertificateGenerationJobs
            .Where(item =>
                item.Status == ParticipationCertificateGenerationJobStatus.Preparing ||
                item.Status == ParticipationCertificateGenerationJobStatus.Processing ||
                item.Status == ParticipationCertificateGenerationJobStatus.CancelRequested ||
                item.Status == ParticipationCertificateGenerationJobStatus.Pending)
            .OrderBy(item => item.Status == ParticipationCertificateGenerationJobStatus.Pending ? 1 : 0)
            .ThenBy(item => item.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (job is null)
            return false;

        if (job.Status == ParticipationCertificateGenerationJobStatus.CancelRequested)
        {
            await MarkJobCancelledAsync(job, cancellationToken);
            return true;
        }

        try
        {
            if (job.Status is ParticipationCertificateGenerationJobStatus.Pending or ParticipationCertificateGenerationJobStatus.Preparing)
                job = await MaterializeGenerationJobAsync(job, cancellationToken);

            if (job.Status == ParticipationCertificateGenerationJobStatus.Cancelled)
                return true;

            await ProcessGenerationJobAsync(job, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _context.ChangeTracker.Clear();
            ParticipationCertificateGenerationJob? failedJob = await _context.ParticipationCertificateGenerationJobs
                .FirstOrDefaultAsync(item => item.Id == job.Id, CancellationToken.None);
            if (failedJob is not null)
            {
                failedJob.Status = ParticipationCertificateGenerationJobStatus.Failed;
                failedJob.LastError = TruncateError(exception.Message);
                failedJob.CompletedAt = DateTime.UtcNow;
                failedJob.HeartbeatAt = DateTime.UtcNow;
                failedJob.UpdatedDate = DateTime.UtcNow;
                failedJob.UpdatedBy = "ParticipationCertificateGenerationWorker";
                await _context.SaveChangesAsync(CancellationToken.None);
            }
        }

        return true;
    }

    private async Task<ParticipationCertificateGenerationJob> MaterializeGenerationJobAsync(
        ParticipationCertificateGenerationJob job,
        CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;
        job.Status = ParticipationCertificateGenerationJobStatus.Preparing;
        job.StartedAt ??= now;
        job.HeartbeatAt = now;
        job.LastError = null;
        job.UpdatedDate = now;
        job.UpdatedBy = "ParticipationCertificateGenerationWorker";
        await _context.SaveChangesAsync(cancellationToken);

        await _context.ParticipationCertificateGenerationJobItems
            .Where(item => item.JobId == job.Id)
            .ExecuteDeleteAsync(cancellationToken);

        ParticipationCertificateDashboardFilter filter = new()
        {
            CertificateCulture = job.Culture,
            SubmissionStatusCode = job.SubmissionStatusCode,
            PaymentStatusCode = job.PaymentStatusCode
        };

        IReadOnlyList<ParticipationCertificateCandidateDto> allCandidates = await BuildCandidatesAsync(
            job.CongressId,
            job.Culture,
            job.Culture,
            filter,
            cancellationToken);

        List<ParticipationCertificateCandidateDto> eligibleCandidates = allCandidates
            .Where(candidate => candidate.IsEligible)
            .ToList();

        HashSet<string> selectedKeys = DeserializeCandidateKeys(job.SelectedCandidateKeysJson);
        HashSet<string> excludedKeys = DeserializeCandidateKeys(job.ExcludedCandidateKeysJson);
        HashSet<Guid> selectedSubmissionIds = ParseSubmissionIds(selectedKeys);
        HashSet<Guid> excludedSubmissionIds = ParseSubmissionIds(excludedKeys);
        IReadOnlyList<ParticipationCertificateSubmissionCandidateDto> submissionCandidates =
            BuildSubmissionCandidates(eligibleCandidates);
        HashSet<Guid> generationSubmissionIds;

        if (job.SelectAllFiltered)
        {
            // Select-all anındaki arama kapsamı job üzerinde snapshot olarak tutulur.
            // Kullanıcı daha sonra bildiri numarasıyla arama yapıp bazı bildirileri seçimden
            // çıkarabilir; son ekrandaki arama seçimin kapsamını değiştirmez.
            IReadOnlyList<ParticipationCertificateSubmissionCandidateDto> searchedSubmissions =
                ApplySubmissionCandidateSearch(submissionCandidates, job.CandidateSearch);

            generationSubmissionIds = searchedSubmissions
                .Where(submission => !excludedSubmissionIds.Contains(submission.SubmissionId))
                .Select(submission => submission.SubmissionId)
                .ToHashSet();
        }
        else
        {
            generationSubmissionIds = selectedSubmissionIds;
        }

        List<ParticipationCertificateCandidateDto> candidates = eligibleCandidates
            .Where(candidate => generationSubmissionIds.Contains(candidate.SubmissionId))
            .ToList();

        job.TotalCount = candidates.Count;
        job.ProcessedCount = 0;
        job.SucceededCount = 0;
        job.FailedCount = 0;
        job.SkippedCount = Math.Max(0, eligibleCandidates.Count - candidates.Count);
        job.MaterializedAt = null;
        job.HeartbeatAt = DateTime.UtcNow;
        job.Status = candidates.Count == 0
            ? ParticipationCertificateGenerationJobStatus.Completed
            : ParticipationCertificateGenerationJobStatus.Preparing;
        job.CompletedAt = candidates.Count == 0 ? DateTime.UtcNow : null;
        job.UpdatedDate = DateTime.UtcNow;
        job.UpdatedBy = "ParticipationCertificateGenerationWorker";
        await _context.SaveChangesAsync(cancellationToken);

        if (candidates.Count == 0)
            return job;

        for (int offset = 0; offset < candidates.Count; offset += MaterializationBatchSize)
        {
            List<ParticipationCertificateGenerationJobItem> items = candidates
                .Skip(offset)
                .Take(MaterializationBatchSize)
                .Select(candidate => new ParticipationCertificateGenerationJobItem
                {
                    Id = Guid.NewGuid(),
                    JobId = job.Id,
                    SubmissionId = candidate.SubmissionId,
                    AuthorId = candidate.AuthorId,
                    SubmissionNumber = candidate.SubmissionNumber,
                    SubmissionTitle = candidate.SubmissionTitle,
                    SubmissionTypeName = candidate.SubmissionTypeName,
                    AuthorDisplayName = candidate.AuthorDisplayNameWithTitle,
                    AuthorEmail = candidate.AuthorEmail,
                    AuthorInstitution = candidate.AuthorInstitution,
                    IsVideoPresentation = candidate.IsVideoPresentation,
                    Status = ParticipationCertificateGenerationItemStatus.Pending,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "ParticipationCertificateGenerationWorker"
                })
                .ToList();

            _context.ParticipationCertificateGenerationJobItems.AddRange(items);
            await _context.SaveChangesAsync(cancellationToken);
            _context.ChangeTracker.Clear();
        }

        _context.ChangeTracker.Clear();
        ParticipationCertificateGenerationJob materializedJob = await _context.ParticipationCertificateGenerationJobs
            .FirstAsync(item => item.Id == job.Id, cancellationToken);
        materializedJob.MaterializedAt = DateTime.UtcNow;
        materializedJob.Status = ParticipationCertificateGenerationJobStatus.Processing;
        materializedJob.HeartbeatAt = DateTime.UtcNow;
        materializedJob.UpdatedDate = DateTime.UtcNow;
        materializedJob.UpdatedBy = "ParticipationCertificateGenerationWorker";
        await _context.SaveChangesAsync(cancellationToken);
        return materializedJob;
    }

    private async Task ProcessGenerationJobAsync(
        ParticipationCertificateGenerationJob job,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ParticipationCertificateTemplate> templates = await GetActiveTemplatesAsync(job.CongressId, cancellationToken);
        ParticipationCertificateTemplate template = templates.FirstOrDefault(item =>
                string.Equals(item.Culture, job.Culture, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Aktif katılım belgesi template PDF dosyası bulunamadı.");
        string certificateBodyText = NormalizeCertificateBodyText(template.BodyText);
        if (string.IsNullOrWhiteSpace(certificateBodyText))
            throw new InvalidOperationException("Sertifika metni boş olduğu için üretim işi çalıştırılamadı.");

        byte[] templateBytes = await ReadRequiredObjectBytesAsync(
            template.BucketName,
            template.ObjectName,
            "Aktif katılım belgesi template dosyası depolama alanında bulunamadı. Template PDF dosyasını yeniden yükleyin.",
            "Aktif katılım belgesi template dosyasına erişilemedi. Lütfen template PDF dosyasını yeniden yükleyip tekrar deneyin.",
            cancellationToken);

        ParticipationCertificateSigner? signer = null;
        byte[]? signatureBytes = null;
        if (template.RenderCommitteeSignature)
        {
            signer = await ResolveCommitteeSignerAsync(job.CongressId, job.Culture, cancellationToken);
            signatureBytes = await ReadRequiredObjectBytesAsync(
                signer.SignatureBucketName,
                signer.SignatureObjectName,
                "İmza yetkili kurul üyesinin imza görseli depolama alanında bulunamadı. Kongre Yönetimi > Kurullar bölümünden imza görselini yeniden yükleyin.",
                "İmza yetkili kurul üyesinin imza görseline erişilemedi. Kongre Yönetimi > Kurullar bölümünden imza görselini yeniden yükleyip tekrar deneyin.",
                cancellationToken);
        }

        string bucketName = GetSubmissionsBucketName();
        string actor = job.RequestedByUserId?.ToString("D") ?? "ParticipationCertificateGenerationWorker";

        // Container kapanması veya process restart sırasında Processing kalan kayıtlar
        // güvenli ve idempotent object name sayesinde tekrar kuyruğa alınabilir.
        await _context.ParticipationCertificateGenerationJobItems
            .Where(item => item.JobId == job.Id && item.Status == ParticipationCertificateGenerationItemStatus.Processing)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Status, ParticipationCertificateGenerationItemStatus.Pending)
                .SetProperty(item => item.UpdatedDate, DateTime.UtcNow)
                .SetProperty(item => item.UpdatedBy, "ParticipationCertificateGenerationWorkerResume"),
                cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            _context.ChangeTracker.Clear();
            ParticipationCertificateGenerationJob currentJob = await _context.ParticipationCertificateGenerationJobs
                .FirstAsync(item => item.Id == job.Id, cancellationToken);

            if (currentJob.Status == ParticipationCertificateGenerationJobStatus.CancelRequested)
            {
                await MarkJobCancelledAsync(currentJob, cancellationToken);
                return;
            }

            List<ParticipationCertificateGenerationJobItem> batch = await _context.ParticipationCertificateGenerationJobItems
                .Where(item => item.JobId == job.Id && item.Status == ParticipationCertificateGenerationItemStatus.Pending)
                .OrderBy(item => item.Id)
                .Take(GenerationBatchSize)
                .ToListAsync(cancellationToken);

            if (batch.Count == 0)
            {
                currentJob.Status = currentJob.FailedCount > 0
                    ? ParticipationCertificateGenerationJobStatus.CompletedWithErrors
                    : ParticipationCertificateGenerationJobStatus.Completed;
                currentJob.CompletedAt = DateTime.UtcNow;
                currentJob.HeartbeatAt = DateTime.UtcNow;
                currentJob.UpdatedDate = DateTime.UtcNow;
                currentJob.UpdatedBy = "ParticipationCertificateGenerationWorker";
                await _context.SaveChangesAsync(cancellationToken);
                return;
            }

            DateTime batchNow = DateTime.UtcNow;
            foreach (ParticipationCertificateGenerationJobItem item in batch)
            {
                item.Status = ParticipationCertificateGenerationItemStatus.Processing;
                item.StartedAt = batchNow;
                item.AttemptCount++;
                item.UpdatedDate = batchNow;
                item.UpdatedBy = "ParticipationCertificateGenerationWorker";
            }
            currentJob.Status = ParticipationCertificateGenerationJobStatus.Processing;
            currentJob.HeartbeatAt = batchNow;
            currentJob.UpdatedDate = batchNow;
            currentJob.UpdatedBy = "ParticipationCertificateGenerationWorker";
            await _context.SaveChangesAsync(cancellationToken);

            List<Guid> submissionIds = batch.Select(item => item.SubmissionId).Distinct().ToList();
            List<Guid> authorIds = batch.Select(item => item.AuthorId).Distinct().ToList();
            List<ParticipationCertificate> existingCertificates = await _context.ParticipationCertificates
                .IgnoreQueryFilters()
                .Where(item =>
                    item.CongressId == currentJob.CongressId &&
                    item.Culture == currentJob.Culture &&
                    submissionIds.Contains(item.SubmissionId) &&
                    authorIds.Contains(item.AuthorId))
                .ToListAsync(cancellationToken);
            Dictionary<(Guid SubmissionId, Guid AuthorId), ParticipationCertificate> certificateMap = existingCertificates
                .GroupBy(item => (item.SubmissionId, item.AuthorId))
                .ToDictionary(group => group.Key, group => group.OrderByDescending(item => item.GeneratedAt).First());

            List<SubmissionFile> existingFiles = await _context.SubmissionFiles
                .IgnoreQueryFilters()
                .Where(file => submissionIds.Contains(file.SubmissionId) && file.FileKind == SubmissionFileKind.ParticipationCertificate)
                .ToListAsync(cancellationToken);
            Dictionary<(Guid SubmissionId, string FilePath), SubmissionFile> fileMap = existingFiles
                .Where(file => !string.IsNullOrWhiteSpace(file.FilePath))
                .GroupBy(file => (file.SubmissionId, file.FilePath))
                .ToDictionary(group => group.Key, group => group.OrderByDescending(file => file.CreatedDate).First());

            int succeededInBatch = 0;
            int failedInBatch = 0;

            foreach (ParticipationCertificateGenerationJobItem item in batch)
            {
                try
                {
                    DateTime generatedAt = DateTime.UtcNow;
                    byte[] pdfBytes = _pdfRenderer.Render(new ParticipationCertificatePdfRenderRequest
                    {
                        TemplatePdfBytes = templateBytes,
                        AuthorFullName = item.AuthorDisplayName,
                        SubmissionTypeName = item.SubmissionTypeName,
                        CertificateText = BuildCertificateText(certificateBodyText, item.SubmissionTypeName),
                        NameBoxX = template.NameBoxX,
                        NameBoxY = template.NameBoxY,
                        NameBoxWidth = template.NameBoxWidth,
                        NameBoxHeight = template.NameBoxHeight,
                        NameFontSize = template.NameFontSize,
                        NameFontColorHex = template.NameFontColorHex,
                        CoverPlaceholderBackground = template.CoverPlaceholderBackground,
                        PlaceholderBackgroundColorHex = template.PlaceholderBackgroundColorHex,
                        RenderCommitteeSignature = template.RenderCommitteeSignature,
                        CommitteeSignatureImageBytes = signatureBytes,
                        CommitteeSignatureBoxX = template.CommitteeSignatureBoxX,
                        CommitteeSignatureBoxY = template.CommitteeSignatureBoxY,
                        CommitteeSignatureBoxWidth = template.CommitteeSignatureBoxWidth,
                        CommitteeSignatureBoxHeight = template.CommitteeSignatureBoxHeight,
                        CommitteeSignerFullName = signer?.FullName ?? string.Empty,
                        CommitteeSignerAcademicTitle = signer?.AcademicTitle,
                        CommitteeSignerRole = signer?.RoleTitle
                    });

                    string submissionPart = NormalizeFileNamePart(item.SubmissionNumber);
                    string authorPart = item.AuthorId.ToString("N")[..12];
                    string languageCode = ParticipationCertificateCultures.GetShortCode(currentJob.Culture);
                    string fileName = string.Equals(currentJob.Culture, ParticipationCertificateCultures.English, StringComparison.OrdinalIgnoreCase)
                        ? $"certificate-of-participation-{submissionPart}-{authorPart}-{languageCode}.pdf"
                        : $"katilim-belgesi-{submissionPart}-{authorPart}-{languageCode}.pdf";
                    string objectName = BackOfficeObjectStorageHelper.BuildObjectName(
                        "participation-certificates", "congresses", currentJob.CongressId.ToString("N"), languageCode,
                        "submissions", item.SubmissionId.ToString("N"), "authors", item.AuthorId.ToString("N"), fileName);

                    using MemoryStream content = new(pdfBytes);
                    ObjectStorageUploadResult upload = await _objectStorageService.UploadAsync(
                        new ObjectStorageUploadRequest
                        {
                            BucketName = bucketName,
                            ObjectName = objectName,
                            OriginalFileName = fileName,
                            ContentType = "application/pdf",
                            Size = pdfBytes.Length,
                            Content = content,
                            Metadata = new Dictionary<string, string>
                            {
                                ["module"] = "participation-certificates",
                                ["congress-id"] = currentJob.CongressId.ToString("N"),
                                ["submission-id"] = item.SubmissionId.ToString("N"),
                                ["author-id"] = item.AuthorId.ToString("N"),
                                ["culture"] = currentJob.Culture,
                                ["generation-job-id"] = currentJob.Id.ToString("N")
                            }
                        },
                        cancellationToken);

                    certificateMap.TryGetValue(
                        (item.SubmissionId, item.AuthorId),
                        out ParticipationCertificate? certificate);

                    string? previousObjectName = certificate?.ObjectName;

                    if (certificate is null || certificate.RevokedAt.HasValue || certificate.DeletedDate.HasValue)
                    {
                        // Kaldırılmış belgenin audit kaydını ve eski public linkinin 410 Gone
                        // davranışını koru. Yeni üretim için yeni bir aktif kayıt oluştur.
                        certificate = new ParticipationCertificate
                        {
                            Id = Guid.NewGuid(),
                            CongressId = currentJob.CongressId,
                            SubmissionId = item.SubmissionId,
                            AuthorId = item.AuthorId,
                            CreatedDate = generatedAt,
                            CreatedBy = actor
                        };
                        _context.ParticipationCertificates.Add(certificate);
                        certificateMap[(item.SubmissionId, item.AuthorId)] = certificate;
                    }

                    certificate.TemplateId = template.Id;
                    certificate.Culture = currentJob.Culture;
                    certificate.SubmissionNumber = item.SubmissionNumber;
                    certificate.SubmissionTitleSnapshot = item.SubmissionTitle;
                    certificate.AuthorFullNameSnapshot = item.AuthorDisplayName;
                    certificate.AuthorEmailSnapshot = item.AuthorEmail;
                    certificate.AuthorInstitutionSnapshot = item.AuthorInstitution;
                    certificate.IsVideoPresentation = item.IsVideoPresentation;
                    certificate.FileName = upload.OriginalFileName;
                    certificate.StorageProvider = _storageOptions.Provider;
                    certificate.BucketName = upload.BucketName;
                    certificate.ObjectName = upload.ObjectName;
                    certificate.ContentType = upload.ContentType;
                    certificate.FileSize = upload.Size;
                    certificate.ETag = upload.ETag;
                    certificate.GeneratedAt = generatedAt;
                    certificate.EmailQueuedAt = null;
                    certificate.EmailSentAt = null;
                    certificate.EmailStatus = "Generated";
                    certificate.EmailError = null;
                    certificate.PublicId = null;
                    certificate.PublicAccessTokenHash = null;
                    certificate.PublishedAt = null;
                    certificate.RevokedAt = null;
                    certificate.RevokedByUserId = null;
                    certificate.RevocationReason = null;
                    certificate.DeletedDate = null;
                    certificate.DeletedBy = null;
                    certificate.UpdatedDate = generatedAt;
                    certificate.UpdatedBy = actor;

                    HideSubmissionFileRecordUntilEmailSent(
                        item.SubmissionId,
                        previousObjectName,
                        generatedAt,
                        actor,
                        fileMap);

                    HideSubmissionFileRecordUntilEmailSent(
                        item.SubmissionId,
                        upload.ObjectName,
                        generatedAt,
                        actor,
                        fileMap);

                    item.CertificateId = certificate.Id;
                    item.Status = ParticipationCertificateGenerationItemStatus.Completed;
                    item.CompletedAt = generatedAt;
                    item.LastError = null;
                    item.UpdatedDate = generatedAt;
                    item.UpdatedBy = "ParticipationCertificateGenerationWorker";
                    succeededInBatch++;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    item.Status = ParticipationCertificateGenerationItemStatus.Failed;
                    item.CompletedAt = DateTime.UtcNow;
                    item.LastError = TruncateError(exception.Message);
                    item.UpdatedDate = DateTime.UtcNow;
                    item.UpdatedBy = "ParticipationCertificateGenerationWorker";
                    failedInBatch++;
                }
            }

            currentJob.ProcessedCount += batch.Count;
            currentJob.SucceededCount += succeededInBatch;
            currentJob.FailedCount += failedInBatch;
            currentJob.HeartbeatAt = DateTime.UtcNow;
            currentJob.UpdatedDate = DateTime.UtcNow;
            currentJob.UpdatedBy = "ParticipationCertificateGenerationWorker";
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private static void HideSubmissionFileRecordUntilEmailSent(
        Guid submissionId,
        string? filePath,
        DateTime generatedAt,
        string actor,
        IReadOnlyDictionary<(Guid SubmissionId, string FilePath), SubmissionFile> fileMap)
    {
        if (submissionId == Guid.Empty || string.IsNullOrWhiteSpace(filePath))
            return;

        if (!fileMap.TryGetValue((submissionId, filePath), out SubmissionFile? file))
            return;

        // Katılım belgesi yeniden üretildiğinde önceki gönderilmiş belge kaydı
        // dokümanlar alanında görünmemelidir. Mail gerçekten gönderildiğinde
        // MailOutboxDispatcherHostedService bu kaydı tekrar aktif hale getirir.
        file.IsActive = false;
        file.DeletedDate = generatedAt;
        file.DeletedBy = actor;
        file.UpdatedDate = generatedAt;
        file.UpdatedBy = actor;
    }

    private async Task MarkJobCancelledAsync(
        ParticipationCertificateGenerationJob job,
        CancellationToken cancellationToken)
    {
        job.Status = ParticipationCertificateGenerationJobStatus.Cancelled;
        job.CompletedAt = DateTime.UtcNow;
        job.HeartbeatAt = DateTime.UtcNow;
        job.UpdatedDate = DateTime.UtcNow;
        job.UpdatedBy = "ParticipationCertificateGenerationWorker";
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<ParticipationCertificateCandidateDto> ApplyCandidateSearch(
        IReadOnlyList<ParticipationCertificateCandidateDto> candidates,
        string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return candidates;

        string query = NormalizeCandidateSearch(search);
        return candidates
            .Where(candidate => NormalizeCandidateSearch(string.Join(" ", new[]
            {
                candidate.SubmissionNumber,
                candidate.SubmissionTitle,
                candidate.SubmissionTypeName,
                candidate.AuthorDisplayNameWithTitle,
                candidate.AuthorFullName,
                candidate.AuthorEmail,
                candidate.AuthorInstitution,
                candidate.SubmissionStatusName,
                candidate.PaymentStatusName
            }.Where(value => !string.IsNullOrWhiteSpace(value)))).Contains(query, StringComparison.Ordinal))
            .ToList();
    }

    private static string NormalizeCandidateSearch(string value)
    {
        string normalized = value.Trim().ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
        return string.Concat(normalized.Where(character => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) != System.Globalization.UnicodeCategory.NonSpacingMark));
    }

    private static IReadOnlyList<string> NormalizeCandidateKeys(IEnumerable<string>? values)
    {
        return (values ?? Array.Empty<string>())
            .Select(value => value?.Trim() ?? string.Empty)
            .Where(IsValidCandidateKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxExcludedCandidateCount)
            .ToList();
    }

    private static HashSet<string> DeserializeCandidateKeys(string? json)
    {
        try
        {
            string[] values = JsonSerializer.Deserialize<string[]>(string.IsNullOrWhiteSpace(json) ? "[]" : json) ?? Array.Empty<string>();
            return NormalizeCandidateKeys(values).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static bool IsValidCandidateKey(string value)
    {
        if (Guid.TryParseExact(value, "N", out _))
            return true;

        // Önceki kişi-bazlı işlerden kalabilecek anahtarları okuyabilmek için
        // geriye dönük uyumluluk korunur: submissionId:authorId.
        string[] parts = value.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 2 &&
               Guid.TryParseExact(parts[0], "N", out _) &&
               Guid.TryParseExact(parts[1], "N", out _);
    }

    private static HashSet<Guid> ParseSubmissionIds(IEnumerable<string> keys)
    {
        HashSet<Guid> result = new();

        foreach (string key in keys)
        {
            if (Guid.TryParseExact(key, "N", out Guid submissionId))
            {
                result.Add(submissionId);
                continue;
            }

            string[] parts = key.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && Guid.TryParseExact(parts[0], "N", out submissionId))
                result.Add(submissionId);
        }

        return result;
    }

    private static string? NormalizeFilterCode(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string TruncateError(string? value)
    {
        string text = string.IsNullOrWhiteSpace(value) ? "Bilinmeyen hata." : value.Trim();
        return text.Length <= 2000 ? text : text[..2000];
    }

    private static bool IsActiveGenerationStatus(ParticipationCertificateGenerationJobStatus status)
        => status is ParticipationCertificateGenerationJobStatus.Pending
            or ParticipationCertificateGenerationJobStatus.Preparing
            or ParticipationCertificateGenerationJobStatus.Processing
            or ParticipationCertificateGenerationJobStatus.CancelRequested;

    private static string GetGenerationStatusDisplayName(ParticipationCertificateGenerationJobStatus status)
    {
        return status switch
        {
            ParticipationCertificateGenerationJobStatus.Pending => "Kuyrukta",
            ParticipationCertificateGenerationJobStatus.Preparing => "Adaylar hazırlanıyor",
            ParticipationCertificateGenerationJobStatus.Processing => "Üretiliyor",
            ParticipationCertificateGenerationJobStatus.Completed => "Tamamlandı",
            ParticipationCertificateGenerationJobStatus.CompletedWithErrors => "Hatalarla tamamlandı",
            ParticipationCertificateGenerationJobStatus.Failed => "Başarısız",
            ParticipationCertificateGenerationJobStatus.CancelRequested => "İptal bekleniyor",
            ParticipationCertificateGenerationJobStatus.Cancelled => "İptal edildi",
            _ => status.ToString()
        };
    }

    private static ParticipationCertificateGenerationJobDto MapGenerationJob(ParticipationCertificateGenerationJob job)
    {
        return new ParticipationCertificateGenerationJobDto
        {
            Id = job.Id,
            CongressId = job.CongressId,
            Culture = job.Culture,
            Status = GetGenerationStatusDisplayName(job.Status),
            TotalCount = job.TotalCount,
            ProcessedCount = job.ProcessedCount,
            SucceededCount = job.SucceededCount,
            FailedCount = job.FailedCount,
            SkippedCount = job.SkippedCount,
            ExcludedCount = job.ExcludedCount,
            CreatedAt = job.CreatedDate,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            LastError = job.LastError,
            IsActive = IsActiveGenerationStatus(job.Status)
        };
    }
}
