using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Features.ParticipationCertificates.Services;
using Symplify.BackOffice.Application.Services.Mailing;
using Symplify.BackOffice.Domain.Communication;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Persistence.Services.ParticipationCertificates;

public sealed partial class ParticipationCertificateService
{
    private const string EmailStatusQueueRequested = "QueueRequested";
    private const string EmailStatusQueuePreparing = "QueuePreparing";
    private const string EmailStatusQueued = "Queued";
    private const string EmailStatusSent = "Sent";
    private const string EmailStatusFailed = "Failed";
    private const int EmailRequestUpdateBatchSize = 1000;

    public async Task<ParticipationCertificateOperationResult> RequestEmailQueueAsync(
        ParticipationCertificateEmailQueueInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.CongressId == Guid.Empty)
            throw new InvalidOperationException("Kongre bilgisi geçersiz.");

        string? certificateCulture = string.IsNullOrWhiteSpace(input.CertificateCulture)
            ? null
            : ParticipationCertificateCultures.Normalize(input.CertificateCulture);

        if (certificateCulture is not null && !ParticipationCertificateCultures.IsSupported(certificateCulture))
            throw new InvalidOperationException("Sertifika dili yalnızca Türkçe veya İngilizce olabilir.");

        HashSet<Guid> selectedIds = input.CertificateIds
            .Where(id => id != Guid.Empty)
            .ToHashSet();
        HashSet<Guid> excludedIds = input.ExcludedCertificateIds
            .Where(id => id != Guid.Empty)
            .ToHashSet();

        if (!input.SelectAllFiltered && selectedIds.Count == 0)
            throw new InvalidOperationException("Mail göndermek için en az bir belge seçmelisiniz.");

        IQueryable<ParticipationCertificate> query = _context.ParticipationCertificates
            .AsNoTracking()
            .Where(certificate =>
                certificate.CongressId == input.CongressId &&
                certificate.DeletedDate == null &&
                certificate.RevokedAt == null &&
                certificate.EmailSentAt == null &&
                certificate.AuthorEmailSnapshot != null &&
                certificate.AuthorEmailSnapshot != string.Empty &&
                (certificate.EmailStatus == null ||
                 (certificate.EmailStatus != EmailStatusQueueRequested &&
                  certificate.EmailStatus != EmailStatusQueuePreparing &&
                  certificate.EmailStatus != EmailStatusQueued &&
                  certificate.EmailStatus != EmailStatusSent)));

        if (certificateCulture is not null)
            query = query.Where(certificate => certificate.Culture == certificateCulture);

        if (input.SelectAllFiltered && !string.IsNullOrWhiteSpace(input.CandidateSearch))
        {
            string search = input.CandidateSearch.Trim().ToLower();
            query = query.Where(certificate =>
                certificate.SubmissionNumber.ToLower().Contains(search) ||
                certificate.SubmissionTitleSnapshot.ToLower().Contains(search) ||
                certificate.AuthorFullNameSnapshot.ToLower().Contains(search) ||
                (certificate.AuthorEmailSnapshot != null && certificate.AuthorEmailSnapshot.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(input.EmailStatus))
        {
            string status = input.EmailStatus.Trim();
            query = status.Equals("NotSent", StringComparison.OrdinalIgnoreCase)
                ? query.Where(certificate => certificate.EmailSentAt == null)
                : query.Where(certificate => certificate.EmailStatus == status);
        }

        if (input.SelectAllFiltered)
        {
            if (excludedIds.Count > 0)
                query = query.Where(certificate => !excludedIds.Contains(certificate.Id));
        }
        else
        {
            query = query.Where(certificate => selectedIds.Contains(certificate.Id));
        }

        List<Guid> certificateIds = await query
            .OrderBy(certificate => certificate.SubmissionNumber)
            .ThenBy(certificate => certificate.AuthorFullNameSnapshot)
            .Select(certificate => certificate.Id)
            .ToListAsync(cancellationToken);

        if (certificateIds.Count == 0)
        {
            throw new InvalidOperationException(
                input.SelectAllFiltered
                    ? "Filtrelenen kayıtlarda mail kuyruğuna alınabilecek belge bulunamadı."
                    : "Seçilen belgeler mail için uygun değil, zaten kuyrukta veya daha önce gönderilmiş.");
        }

        DateTime now = DateTime.UtcNow;
        string actor = input.RequestedByUserId?.ToString("D") ?? "ParticipationCertificateEmailQueueRequested";
        int requestedCount = 0;

        foreach (List<Guid> batch in certificateIds.Chunk(EmailRequestUpdateBatchSize).Select(chunk => chunk.ToList()))
        {
            requestedCount += await _context.ParticipationCertificates
                .Where(certificate =>
                    certificate.CongressId == input.CongressId &&
                    certificate.DeletedDate == null &&
                    certificate.RevokedAt == null &&
                    certificate.EmailSentAt == null &&
                    batch.Contains(certificate.Id) &&
                    (certificate.EmailStatus == null ||
                     (certificate.EmailStatus != EmailStatusQueueRequested &&
                      certificate.EmailStatus != EmailStatusQueuePreparing &&
                      certificate.EmailStatus != EmailStatusQueued &&
                      certificate.EmailStatus != EmailStatusSent)))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(certificate => certificate.EmailQueuedAt, now)
                    .SetProperty(certificate => certificate.EmailStatus, EmailStatusQueueRequested)
                    .SetProperty(certificate => certificate.EmailError, (string?)null)
                    .SetProperty(certificate => certificate.UpdatedDate, now)
                    .SetProperty(certificate => certificate.UpdatedBy, actor),
                    cancellationToken);
        }

        if (requestedCount == 0)
            throw new InvalidOperationException("Seçilen belgeler zaten mail kuyruğunda veya daha önce gönderilmiş.");

        return new ParticipationCertificateOperationResult
        {
            CandidateCount = certificateIds.Count,
            EmailQueuedCount = requestedCount,
            SkippedCount = certificateIds.Count - requestedCount,
            Warnings = input.SelectAllFiltered
                ? new[] { "Filtrelenen tüm uygun belgeler arka plan mail kuyruğuna alındı." }
                : Array.Empty<string>()
        };
    }

    public async Task<int> ProcessRequestedEmailQueueBatchAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        int resolvedBatchSize = Math.Clamp(batchSize, 1, 250);
        DateTime now = DateTime.UtcNow;
        DateTime staleThreshold = now.AddMinutes(-10);

        await _context.ParticipationCertificates
            .Where(certificate =>
                certificate.DeletedDate == null &&
                certificate.RevokedAt == null &&
                certificate.EmailSentAt == null &&
                certificate.EmailStatus == EmailStatusQueuePreparing &&
                certificate.UpdatedDate.HasValue &&
                certificate.UpdatedDate.Value < staleThreshold)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(certificate => certificate.EmailStatus, EmailStatusQueueRequested)
                .SetProperty(certificate => certificate.UpdatedDate, now)
                .SetProperty(certificate => certificate.UpdatedBy, "ParticipationCertificateEmailQueueRecovery"),
                cancellationToken);

        List<ParticipationCertificate> certificates = await _context.ParticipationCertificates
            .Where(certificate =>
                certificate.DeletedDate == null &&
                certificate.RevokedAt == null &&
                certificate.EmailSentAt == null &&
                certificate.EmailStatus == EmailStatusQueueRequested)
            .OrderBy(certificate => certificate.EmailQueuedAt)
            .ThenBy(certificate => certificate.SubmissionNumber)
            .ThenBy(certificate => certificate.AuthorFullNameSnapshot)
            .Take(resolvedBatchSize)
            .ToListAsync(cancellationToken);

        if (certificates.Count == 0)
            return 0;

        foreach (ParticipationCertificate certificate in certificates)
        {
            certificate.EmailStatus = EmailStatusQueuePreparing;
            certificate.EmailError = null;
            certificate.UpdatedDate = now;
            certificate.UpdatedBy = "ParticipationCertificateEmailQueueWorker";
        }

        await _context.SaveChangesAsync(cancellationToken);

        List<Guid> certificateIds = certificates.Select(certificate => certificate.Id).ToList();
        HashSet<Guid> pendingCertificateIds = (await _context.MailOutboxMessages
                .AsNoTracking()
                .Where(message =>
                    message.DeletedDate == null &&
                    message.Status == MailOutboxStatus.Pending &&
                    message.ParticipationCertificateId.HasValue &&
                    certificateIds.Contains(message.ParticipationCertificateId.Value))
                .Select(message => message.ParticipationCertificateId!.Value)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        List<Guid> congressIds = certificates.Select(certificate => certificate.CongressId).Distinct().ToList();
        List<ParticipationCertificateTemplate> templates = await _context.ParticipationCertificateTemplates
            .AsNoTracking()
            .Where(template =>
                congressIds.Contains(template.CongressId) &&
                template.IsActive &&
                template.DeletedDate == null)
            .ToListAsync(cancellationToken);
        Dictionary<(Guid CongressId, string Culture), ParticipationCertificateTemplate> templateMap = templates
            .GroupBy(template => (template.CongressId, ParticipationCertificateCultures.Normalize(template.Culture)))
            .ToDictionary(group => group.Key, group => group.OrderByDescending(template => template.UploadedAt).First());

        Dictionary<(Guid CongressId, string Culture), CongressInfo> congressMap = new();
        Dictionary<(Guid CongressId, string Culture), MailBrandingModel> brandingMap = new();
        Dictionary<Guid, ResolvedOrganizationMailConfiguration> senderMap = new();
        DateTime processedAt = DateTime.UtcNow;

        foreach (ParticipationCertificate certificate in certificates)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(certificate.AuthorEmailSnapshot))
                    throw new InvalidOperationException("Yazarın e-posta adresi bulunamadı.");

                if (certificate.EmailSentAt.HasValue ||
                    string.Equals(certificate.EmailStatus, EmailStatusSent, StringComparison.OrdinalIgnoreCase))
                {
                    certificate.EmailStatus = EmailStatusSent;
                    certificate.EmailError = null;
                    continue;
                }

                if (pendingCertificateIds.Contains(certificate.Id))
                {
                    certificate.EmailStatus = EmailStatusQueued;
                    certificate.EmailQueuedAt ??= processedAt;
                    certificate.EmailError = null;
                    continue;
                }

                string culture = ParticipationCertificateCultures.Normalize(certificate.Culture);
                if (!templateMap.TryGetValue((certificate.CongressId, culture), out ParticipationCertificateTemplate? template))
                    throw new InvalidOperationException("Bu dil için aktif katılım belgesi ve mail template kaydı bulunamadı.");

                string mailSubject = NormalizeMailSubject(template.MailSubject);
                string mailTitle = NormalizeMailTitle(template.MailTitle);
                string mailBodyHtml = NormalizeMailBodyHtml(template.MailBodyHtml);
                ValidateMailTemplate(mailSubject, mailBodyHtml);

                if (!congressMap.TryGetValue((certificate.CongressId, culture), out CongressInfo? congress))
                {
                    congress = await ResolveCongressAsync(certificate.CongressId, culture, cancellationToken);
                    congressMap[(certificate.CongressId, culture)] = congress;
                }

                if (!brandingMap.TryGetValue((certificate.CongressId, culture), out MailBrandingModel? branding))
                {
                    branding = await _mailBrandingResolver.ResolveForCongressAsync(
                        certificate.CongressId,
                        culture: culture,
                        cancellationToken: cancellationToken);
                    brandingMap[(certificate.CongressId, culture)] = branding;
                }

                if (!senderMap.TryGetValue(congress.OrganizationId, out ResolvedOrganizationMailConfiguration? sender))
                {
                    sender = await _mailConfigurationResolver.ResolveAsync(
                        congress.OrganizationId,
                        cancellationToken);
                    senderMap[congress.OrganizationId] = sender;
                }

                Guid publicId = Guid.NewGuid();
                string rawToken = CreatePublicAccessToken();
                string tokenHash = HashPublicAccessToken(rawToken);
                string publicUrl = BuildPublicCertificateUrl(publicId, rawToken);

                certificate.PublicId = publicId;
                certificate.PublicAccessTokenHash = tokenHash;
                certificate.PublishedAt = null;
                certificate.RevokedAt = null;
                certificate.RevokedByUserId = null;
                certificate.RevocationReason = null;

                string renderedSubject = ReplaceMailTemplateTokens(
                    mailSubject,
                    certificate,
                    congress.Title,
                    culture,
                    publicUrl,
                    htmlEncodeValues: false);
                string renderedTitle = ReplaceMailTemplateTokens(
                    string.IsNullOrWhiteSpace(mailTitle) ? mailSubject : mailTitle,
                    certificate,
                    congress.Title,
                    culture,
                    publicUrl,
                    htmlEncodeValues: false);
                string renderedBody = ReplaceMailTemplateTokens(
                    mailBodyHtml,
                    certificate,
                    congress.Title,
                    culture,
                    publicUrl,
                    htmlEncodeValues: true);

                RenderedSystemMailTemplate rendered = await _mailTemplateRenderer.RenderCustomAsync(
                    new CustomMailTemplateRenderRequest
                    {
                        Culture = culture,
                        Subject = renderedSubject,
                        Title = renderedTitle,
                        SafeBodyHtml = renderedBody,
                        Branding = branding
                    },
                    cancellationToken);

                MailOutboxMessage message = new()
                {
                    Id = Guid.NewGuid(),
                    MailType = MailMessageType.ParticipationCertificate,
                    RelatedAuthorId = certificate.AuthorId,
                    RelatedSubmissionId = certificate.SubmissionId,
                    ParticipationCertificateId = certificate.Id,
                    ContainsSensitiveContent = true,
                    ToEmail = certificate.AuthorEmailSnapshot.Trim(),
                    ToName = certificate.AuthorFullNameSnapshot,
                    Subject = rendered.Subject,
                    HtmlBody = rendered.HtmlBody,
                    AttachmentPath = null,
                    AttachmentBucketName = null,
                    AttachmentObjectName = null,
                    AttachmentFileName = null,
                    AttachmentContentType = null,
                    Status = MailOutboxStatus.Pending,
                    OrganizationId = congress.OrganizationId,
                    CongressId = certificate.CongressId,
                    FromEmail = sender.FromEmail,
                    FromName = sender.FromName,
                    ReplyToEmail = sender.ReplyToEmail,
                    ReplyToName = sender.ReplyToName,
                    CreatedDate = processedAt,
                    CreatedBy = "ParticipationCertificateEmailQueueWorker"
                };

                _context.MailOutboxMessages.Add(message);
                certificate.EmailQueuedAt = processedAt;
                certificate.EmailStatus = EmailStatusQueued;
                certificate.EmailError = null;
                pendingCertificateIds.Add(certificate.Id);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                certificate.EmailStatus = EmailStatusFailed;
                certificate.EmailError = exception.Message.Length > 1000
                    ? exception.Message[..1000]
                    : exception.Message;
            }

            certificate.UpdatedDate = DateTime.UtcNow;
            certificate.UpdatedBy = "ParticipationCertificateEmailQueueWorker";
        }

        await _context.SaveChangesAsync(cancellationToken);
        return certificates.Count;
    }

    private string BuildPublicCertificateUrl(Guid publicId, string token)
    {
        return _publicUrlService.Build(
            $"/public/certificates/{publicId:D}/{Uri.EscapeDataString(token)}");
    }

    private static string CreatePublicAccessToken()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string HashPublicAccessToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string ReplaceMailTemplateTokens(
        string template,
        ParticipationCertificate certificate,
        string congressTitle,
        string culture,
        string publicUrl,
        bool htmlEncodeValues)
    {
        string Encode(string? value)
            => htmlEncodeValues ? WebUtility.HtmlEncode(value ?? string.Empty) : value ?? string.Empty;

        string language = string.Equals(culture, ParticipationCertificateCultures.English, StringComparison.OrdinalIgnoreCase)
            ? "English"
            : "Türkçe";

        return template
            .Replace("{{AUTHOR_NAME}}", Encode(certificate.AuthorFullNameSnapshot), StringComparison.OrdinalIgnoreCase)
            .Replace("{{CONGRESS_NAME}}", Encode(congressTitle), StringComparison.OrdinalIgnoreCase)
            .Replace("{{SUBMISSION_NUMBER}}", Encode(certificate.SubmissionNumber), StringComparison.OrdinalIgnoreCase)
            .Replace("{{SUBMISSION_TITLE}}", Encode(certificate.SubmissionTitleSnapshot), StringComparison.OrdinalIgnoreCase)
            .Replace("{{CERTIFICATE_LANGUAGE}}", Encode(language), StringComparison.OrdinalIgnoreCase)
            .Replace("{{CERTIFICATE_LINK}}", Encode(publicUrl), StringComparison.OrdinalIgnoreCase);
    }

}
