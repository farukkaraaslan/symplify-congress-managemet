using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Symplify.BackOffice.Application.Common.Storage;
using Symplify.BackOffice.Application.Features.ParticipationCertificates.Services;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Persistence.Services.ParticipationCertificates;

public sealed partial class ParticipationCertificateService
{
    public async Task<ParticipationCertificateCandidatePageResult> GetCandidatePageAsync(
        ParticipationCertificateCandidatePageRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.CongressId == Guid.Empty)
            return new ParticipationCertificateCandidatePageResult();

        ParticipationCertificateDashboardFilter filter = new()
        {
            SubmissionStatusCode = NormalizeFilterCode(request.SubmissionStatusCode),
            PaymentStatusCode = NormalizeFilterCode(request.PaymentStatusCode)
        };

        IReadOnlyList<ParticipationCertificateCandidateDto> authorCandidates = await BuildCandidatesAsync(
            request.CongressId,
            request.DisplayCulture,
            ParticipationCertificateCultures.Turkish,
            filter,
            cancellationToken);

        IReadOnlyList<ParticipationCertificateSubmissionCandidateDto> submissions = BuildSubmissionCandidates(authorCandidates);
        int totalCount = submissions.Count;
        IReadOnlyList<ParticipationCertificateSubmissionCandidateDto> filtered = ApplySubmissionCandidateSearch(
            submissions,
            request.SearchText);

        IEnumerable<ParticipationCertificateSubmissionCandidateDto> ordered = ApplyCandidateOrdering(
            filtered,
            request.SortColumn,
            request.SortDirection);

        int length = Math.Clamp(request.Length <= 0 ? 25 : request.Length, 10, 250);
        int start = Math.Max(0, request.Start);

        return new ParticipationCertificateCandidatePageResult
        {
            TotalCount = totalCount,
            FilteredCount = filtered.Count,
            Items = ordered.Skip(start).Take(length).ToList()
        };
    }

    public async Task<ParticipationCertificateDocumentPageResult> GetDocumentPageAsync(
        ParticipationCertificateDocumentPageRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.CongressId == Guid.Empty)
            return new ParticipationCertificateDocumentPageResult();

        IQueryable<ParticipationCertificate> scope = request.IncludeRevoked
            ? _context.ParticipationCertificates
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(item =>
                    item.CongressId == request.CongressId &&
                    (item.DeletedDate == null || item.RevokedAt != null))
            : _context.ParticipationCertificates
                .AsNoTracking()
                .Where(item => item.CongressId == request.CongressId && item.DeletedDate == null);

        int totalCount = await scope.CountAsync(cancellationToken);

        string? culture = string.IsNullOrWhiteSpace(request.CertificateCulture)
            ? null
            : ParticipationCertificateCultures.Normalize(request.CertificateCulture);
        if (!string.IsNullOrWhiteSpace(culture))
            scope = scope.Where(item => item.Culture == culture);

        if (!string.IsNullOrWhiteSpace(request.EmailStatus))
        {
            string emailStatus = request.EmailStatus.Trim();
            scope = emailStatus.Equals("NotSent", StringComparison.OrdinalIgnoreCase)
                ? scope.Where(item => item.EmailSentAt == null && item.RevokedAt == null)
                : emailStatus.Equals("Sent", StringComparison.OrdinalIgnoreCase)
                    ? scope.Where(item => item.EmailSentAt != null && item.RevokedAt == null)
                    : emailStatus.Equals("Revoked", StringComparison.OrdinalIgnoreCase)
                        ? scope.Where(item => item.RevokedAt != null)
                        : scope.Where(item => item.EmailStatus == emailStatus);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            string search = request.SearchText.Trim().ToLower();
            scope = scope.Where(item =>
                item.SubmissionNumber.ToLower().Contains(search) ||
                item.SubmissionTitleSnapshot.ToLower().Contains(search) ||
                item.AuthorFullNameSnapshot.ToLower().Contains(search) ||
                (item.AuthorEmailSnapshot != null && item.AuthorEmailSnapshot.ToLower().Contains(search)));
        }

        int filteredCount = await scope.CountAsync(cancellationToken);
        scope = ApplyDocumentOrdering(scope, request.SortColumn, request.SortDirection);

        int length = Math.Clamp(request.Length <= 0 ? 25 : request.Length, 10, 250);
        int start = Math.Max(0, request.Start);

        List<ParticipationCertificateDocumentDto> items = await scope
            .Skip(start)
            .Take(length)
            .Select(item => new ParticipationCertificateDocumentDto
            {
                Id = item.Id,
                SubmissionId = item.SubmissionId,
                AuthorId = item.AuthorId,
                SubmissionNumber = item.SubmissionNumber,
                SubmissionTitle = item.SubmissionTitleSnapshot,
                AuthorFullName = item.AuthorFullNameSnapshot,
                AuthorEmail = item.AuthorEmailSnapshot,
                Culture = item.Culture,
                FileName = item.FileName,
                GeneratedAt = item.GeneratedAt,
                EmailQueuedAt = item.EmailQueuedAt,
                EmailSentAt = item.EmailSentAt,
                EmailStatus = item.EmailStatus,
                PublishedAt = item.PublishedAt,
                RevokedAt = item.RevokedAt,
                RevocationReason = item.RevocationReason
            })
            .ToListAsync(cancellationToken);

        return new ParticipationCertificateDocumentPageResult
        {
            TotalCount = totalCount,
            FilteredCount = filteredCount,
            Items = items
        };
    }

    public async Task<ParticipationCertificatePublicAccessResult> ResolvePublicAccessAsync(
        Guid publicId,
        string token,
        CancellationToken cancellationToken = default)
    {
        if (publicId == Guid.Empty || string.IsNullOrWhiteSpace(token) || token.Length > 256)
        {
            return new ParticipationCertificatePublicAccessResult
            {
                Status = ParticipationCertificatePublicAccessStatus.NotFound
            };
        }

        ParticipationCertificate? certificate = await _context.ParticipationCertificates
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.PublicId == publicId, cancellationToken);

        if (certificate is null || string.IsNullOrWhiteSpace(certificate.PublicAccessTokenHash))
        {
            return new ParticipationCertificatePublicAccessResult
            {
                Status = ParticipationCertificatePublicAccessStatus.NotFound
            };
        }

        if (!PublicTokenMatches(token, certificate.PublicAccessTokenHash))
        {
            return new ParticipationCertificatePublicAccessResult
            {
                Status = ParticipationCertificatePublicAccessStatus.InvalidToken
            };
        }

        if (certificate.RevokedAt.HasValue || certificate.DeletedDate.HasValue)
        {
            return new ParticipationCertificatePublicAccessResult
            {
                Status = ParticipationCertificatePublicAccessStatus.Revoked,
                Message = "Bu katılım belgesi kongre yönetimi tarafından kaldırılmıştır."
            };
        }

        if (!certificate.PublishedAt.HasValue || !certificate.EmailSentAt.HasValue)
        {
            return new ParticipationCertificatePublicAccessResult
            {
                Status = ParticipationCertificatePublicAccessStatus.NotPublished,
                Message = "Bu katılım belgesi henüz yayınlanmamıştır."
            };
        }

        return new ParticipationCertificatePublicAccessResult
        {
            Status = ParticipationCertificatePublicAccessStatus.Available,
            File = new ParticipationCertificateStoredFileDto
            {
                Id = certificate.Id,
                CongressId = certificate.CongressId,
                SubmissionNumber = certificate.SubmissionNumber,
                AuthorFullName = certificate.AuthorFullNameSnapshot,
                Culture = certificate.Culture,
                FileName = certificate.FileName,
                ContentType = certificate.ContentType,
                BucketName = certificate.BucketName,
                ObjectName = certificate.ObjectName
            }
        };
    }

    public async Task<ParticipationCertificateRevokeResult> RevokeAsync(
        Guid certificateId,
        string? reason,
        Guid? performedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (certificateId == Guid.Empty)
            throw new InvalidOperationException("Katılım belgesi bilgisi geçersiz.");

        ParticipationCertificate certificate = await _context.ParticipationCertificates
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == certificateId, cancellationToken)
            ?? throw new InvalidOperationException("Katılım belgesi bulunamadı.");

        if (certificate.RevokedAt.HasValue)
        {
            return new ParticipationCertificateRevokeResult
            {
                CertificateId = certificate.Id,
                BucketName = certificate.BucketName,
                ObjectName = certificate.ObjectName,
                AlreadyRevoked = true
            };
        }

        DateTime now = DateTime.UtcNow;
        string actor = performedByUserId?.ToString("D") ?? "ParticipationCertificateRevoked";
        string normalizedReason = string.IsNullOrWhiteSpace(reason)
            ? "Yönetim panelinden kaldırıldı."
            : reason.Trim();
        if (normalizedReason.Length > 1000)
            normalizedReason = normalizedReason[..1000];

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        certificate.PublishedAt = null;
        certificate.RevokedAt = now;
        certificate.RevokedByUserId = performedByUserId;
        certificate.RevocationReason = normalizedReason;
        certificate.EmailStatus = "Revoked";
        certificate.EmailError = normalizedReason;
        certificate.DeletedDate = now;
        certificate.DeletedBy = actor;
        certificate.UpdatedDate = now;
        certificate.UpdatedBy = actor;

        List<SubmissionFile> submissionFiles = await _context.SubmissionFiles
            .IgnoreQueryFilters()
            .Where(file =>
                file.SubmissionId == certificate.SubmissionId &&
                file.FileKind == SubmissionFileKind.ParticipationCertificate &&
                file.FilePath == certificate.ObjectName)
            .ToListAsync(cancellationToken);

        foreach (SubmissionFile file in submissionFiles)
        {
            file.IsActive = false;
            file.DeletedDate = now;
            file.DeletedBy = actor;
            file.UpdatedDate = now;
            file.UpdatedBy = actor;
        }

        await _context.MailOutboxMessages
            .Where(message =>
                message.ParticipationCertificateId == certificate.Id &&
                message.Status == MailOutboxStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(message => message.Status, MailOutboxStatus.Cancelled)
                .SetProperty(message => message.HtmlBody, "<p>Katılım belgesi kaldırıldığı için public link maili iptal edildi.</p>")
                .SetProperty(message => message.LastError, "Katılım belgesi yönetici tarafından kaldırıldı.")
                .SetProperty(message => message.UpdatedDate, now)
                .SetProperty(message => message.UpdatedBy, actor),
                cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        bool storageDeleteSucceeded = true;
        try
        {
            // Veritabanı transaction'ı bu noktada commit edildi. İstemci bağlantısı kesilse bile
            // kaldırma işlemini başarısız göstermemek ve orphan dosya bırakmamak için storage
            // temizliğini request cancellation tokenından bağımsız, best-effort yürüt.
            await BackOfficeObjectStorageHelper.DeleteObjectIfExistsAsync(
                _objectStorageService,
                certificate.BucketName,
                certificate.ObjectName,
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            storageDeleteSucceeded = false;
            // Link ve doküman erişimi transaction içinde zaten iptal edildi. Storage temizliği
            // geçici olarak başarısız olsa bile kaldırma işlemini geri döndürmeyiz.
            _logger.LogWarning(
                exception,
                "Revoked participation certificate object could not be deleted. CertificateId: {CertificateId}, Bucket: {Bucket}, Object: {Object}",
                certificate.Id,
                certificate.BucketName,
                certificate.ObjectName);
        }

        return new ParticipationCertificateRevokeResult
        {
            CertificateId = certificate.Id,
            BucketName = certificate.BucketName,
            ObjectName = certificate.ObjectName,
            StorageDeleteSucceeded = storageDeleteSucceeded
        };
    }

    private static IEnumerable<ParticipationCertificateSubmissionCandidateDto> ApplyCandidateOrdering(
        IEnumerable<ParticipationCertificateSubmissionCandidateDto> source,
        string? sortColumn,
        string? sortDirection)
    {
        bool descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        string column = sortColumn?.Trim().ToLowerInvariant() ?? "submissionnumber";

        return column switch
        {
            "title" => descending
                ? source.OrderByDescending(item => item.SubmissionTitle)
                : source.OrderBy(item => item.SubmissionTitle),
            "authors" => descending
                ? source.OrderByDescending(item => item.AuthorCount).ThenByDescending(item => item.AuthorNames)
                : source.OrderBy(item => item.AuthorCount).ThenBy(item => item.AuthorNames),
            "status" => descending
                ? source.OrderByDescending(item => item.SubmissionStatusName)
                : source.OrderBy(item => item.SubmissionStatusName),
            "certificate" => descending
                ? source.OrderByDescending(item => item.TurkishCertificateCount + item.EnglishCertificateCount)
                : source.OrderBy(item => item.TurkishCertificateCount + item.EnglishCertificateCount),
            _ => descending
                ? source.OrderByDescending(item => item.SubmissionNumber)
                : source.OrderBy(item => item.SubmissionNumber)
        };
    }

    private static IReadOnlyList<ParticipationCertificateSubmissionCandidateDto> BuildSubmissionCandidates(
        IReadOnlyList<ParticipationCertificateCandidateDto> authorCandidates)
    {
        return authorCandidates
            .GroupBy(candidate => candidate.SubmissionId)
            .Select(group =>
            {
                ParticipationCertificateCandidateDto first = group.First();
                List<ParticipationCertificateCandidateDto> authors = group
                    .OrderBy(candidate => candidate.AuthorDisplayNameWithTitle)
                    .ToList();

                return new ParticipationCertificateSubmissionCandidateDto
                {
                    SubmissionId = first.SubmissionId,
                    SubmissionNumber = first.SubmissionNumber,
                    SubmissionTitle = first.SubmissionTitle,
                    SubmissionTypeName = first.SubmissionTypeName,
                    SubmissionStatusCode = first.SubmissionStatusCode,
                    SubmissionStatusName = first.SubmissionStatusName,
                    PaymentStatusCode = first.PaymentStatusCode,
                    PaymentStatusName = first.PaymentStatusName,
                    IsEligible = authors.Count > 0 && authors.All(candidate => candidate.IsEligible),
                    IsVideoPresentation = first.IsVideoPresentation,
                    AuthorCount = authors.Count,
                    EligibleAuthorCount = authors.Count(candidate => candidate.IsEligible),
                    AuthorNames = string.Join(", ", authors.Select(candidate => candidate.AuthorDisplayNameWithTitle)),
                    AuthorEmails = string.Join(", ", authors
                        .Select(candidate => candidate.AuthorEmail)
                        .Where(email => !string.IsNullOrWhiteSpace(email))
                        .Distinct(StringComparer.OrdinalIgnoreCase)),
                    Institutions = string.Join(", ", authors
                        .Select(candidate => candidate.AuthorInstitution)
                        .Where(institution => !string.IsNullOrWhiteSpace(institution))
                        .Distinct(StringComparer.OrdinalIgnoreCase)),
                    TurkishCertificateCount = authors.Count(candidate => candidate.HasTurkishCertificate),
                    EnglishCertificateCount = authors.Count(candidate => candidate.HasEnglishCertificate)
                };
            })
            .OrderByDescending(candidate => candidate.IsEligible)
            .ThenBy(candidate => candidate.SubmissionNumber)
            .ToList();
    }

    private static IReadOnlyList<ParticipationCertificateSubmissionCandidateDto> ApplySubmissionCandidateSearch(
        IReadOnlyList<ParticipationCertificateSubmissionCandidateDto> submissions,
        string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return submissions;

        string query = NormalizeCandidateSearch(search);
        return submissions
            .Where(submission => NormalizeCandidateSearch(string.Join(" ", new[]
            {
                submission.SubmissionNumber,
                submission.SubmissionTitle,
                submission.SubmissionTypeName,
                submission.AuthorNames,
                submission.AuthorEmails,
                submission.Institutions,
                submission.SubmissionStatusName,
                submission.PaymentStatusName
            }.Where(value => !string.IsNullOrWhiteSpace(value)))).Contains(query, StringComparison.Ordinal))
            .ToList();
    }

    private static IQueryable<ParticipationCertificate> ApplyDocumentOrdering(
        IQueryable<ParticipationCertificate> query,
        string? sortColumn,
        string? sortDirection)
    {
        bool descending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase) == false;
        string column = sortColumn?.Trim().ToLowerInvariant() ?? "generatedat";

        return column switch
        {
            "submissionnumber" => descending
                ? query.OrderByDescending(item => item.SubmissionNumber).ThenByDescending(item => item.AuthorFullNameSnapshot)
                : query.OrderBy(item => item.SubmissionNumber).ThenBy(item => item.AuthorFullNameSnapshot),
            "author" => descending
                ? query.OrderByDescending(item => item.AuthorFullNameSnapshot)
                : query.OrderBy(item => item.AuthorFullNameSnapshot),
            "culture" => descending
                ? query.OrderByDescending(item => item.Culture)
                : query.OrderBy(item => item.Culture),
            "emailstatus" => descending
                ? query.OrderByDescending(item => item.EmailStatus)
                : query.OrderBy(item => item.EmailStatus),
            _ => descending
                ? query.OrderByDescending(item => item.GeneratedAt)
                : query.OrderBy(item => item.GeneratedAt)
        };
    }

    private static bool PublicTokenMatches(string token, string storedHash)
    {
        try
        {
            byte[] expected = Convert.FromHexString(storedHash);
            byte[] actual = SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()));
            return expected.Length == actual.Length && CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
