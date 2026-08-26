using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Services.Email;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Communication;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Infrastructure.Email;

public sealed class MailOutboxDispatcherHostedService : BackgroundService
{
    private const string ParticipationCertificateSentFilePendingStatus = "SentFilePending";
    private const string ParticipationCertificateSentStatus = "Sent";
    private const string ParticipationCertificateQueuedStatus = "Queued";
    private const string ParticipationCertificateFailedStatus = "Failed";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MailOutboxDispatcherHostedService> _logger;
    private readonly BackOfficeMailOptions _options;

    public MailOutboxDispatcherHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<BackOfficeMailOptions> options,
        ILogger<MailOutboxDispatcherHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Outbox.Enabled)
            return;

        TimeSpan interval = TimeSpan.FromSeconds(Math.Max(5, _options.Outbox.IntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchPendingMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Mail outbox dispatch cycle failed.");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task DispatchPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();

        IMailOutboxMessageRepository repository = scope.ServiceProvider.GetRequiredService<IMailOutboxMessageRepository>();
        IParticipationCertificateRepository participationCertificateRepository =
            scope.ServiceProvider.GetRequiredService<IParticipationCertificateRepository>();
        ISubmissionFileRepository submissionFileRepository =
            scope.ServiceProvider.GetRequiredService<ISubmissionFileRepository>();
        ISubmissionAcceptanceLetterRepository acceptanceLetterRepository =
            scope.ServiceProvider.GetRequiredService<ISubmissionAcceptanceLetterRepository>();
        IBackOfficeEmailSender emailSender = scope.ServiceProvider.GetRequiredService<IBackOfficeEmailSender>();

        int batchSize = Math.Clamp(_options.Outbox.BatchSize, 1, 100);
        int maxAttemptCount = Math.Clamp(_options.Outbox.MaxAttemptCount, 1, 20);

        await RecoverSentParticipationCertificateStatesAsync(
            repository,
            participationCertificateRepository,
            submissionFileRepository,
            batchSize,
            cancellationToken);

        await PublishPendingParticipationCertificateFilesAsync(
            participationCertificateRepository,
            submissionFileRepository,
            batchSize,
            cancellationToken);

        var pendingMessages = await repository.GetListAsync(
            predicate: message =>
                message.Status == MailOutboxStatus.Pending &&
                message.AttemptCount < maxAttemptCount,
            orderBy: query => query.OrderBy(message => message.CreatedDate),
            index: 0,
            size: batchSize,
            cancellationToken: cancellationToken);

        foreach (MailOutboxMessage message in pendingMessages.Items)
        {
            await DispatchMessageAsync(
                repository,
                participationCertificateRepository,
                submissionFileRepository,
                acceptanceLetterRepository,
                emailSender,
                message,
                maxAttemptCount,
                cancellationToken);
        }
    }

    private async Task DispatchMessageAsync(
        IMailOutboxMessageRepository repository,
        IParticipationCertificateRepository participationCertificateRepository,
        ISubmissionFileRepository submissionFileRepository,
        ISubmissionAcceptanceLetterRepository acceptanceLetterRepository,
        IBackOfficeEmailSender emailSender,
        MailOutboxMessage message,
        int maxAttemptCount,
        CancellationToken cancellationToken)
    {
        ParticipationCertificate? certificate = await FindParticipationCertificateAsync(
            participationCertificateRepository,
            message,
            cancellationToken);

        // Mail henüz gönderilmeden belge kaldırılmışsa outbox kaydını sessizce iptal et.
        if (message.ParticipationCertificateId.HasValue &&
            (certificate is null || certificate.RevokedAt.HasValue || certificate.DeletedDate.HasValue))
        {
            message.Status = MailOutboxStatus.Cancelled;
            message.LastError = "Katılım belgesi kaldırıldığı için mail gönderimi iptal edildi.";
            message.UpdatedDate = DateTime.UtcNow;
            message.UpdatedBy = "MailOutboxDispatcher";
            await repository.UpdateAsync(message);
            return;
        }

        message.AttemptCount++;
        message.LastAttemptAt = DateTime.UtcNow;
        message.UpdatedDate = DateTime.UtcNow;
        message.UpdatedBy = "MailOutboxDispatcher";

        bool transportPersistedAtomically = false;

        try
        {
            BackOfficeEmailSendResult sendResult = await emailSender.SendAsync(
                new BackOfficeEmailMessage
                {
                    TrackingId = message.Id,
                    MailType = message.MailType,
                    OrganizationId = message.OrganizationId
                        ?? throw new InvalidOperationException($"Mail outbox message {message.Id} has no OrganizationId."),
                    CongressId = message.CongressId,
                    FromEmail = message.FromEmail,
                    FromName = message.FromName,
                    ReplyToEmail = message.ReplyToEmail,
                    ReplyToName = message.ReplyToName,
                    ToEmail = message.ToEmail,
                    ToName = message.ToName,
                    Subject = message.Subject,
                    HtmlBody = message.HtmlBody,
                    AttachmentPath = message.AttachmentPath,
                    AttachmentBucketName = message.AttachmentBucketName,
                    AttachmentObjectName = message.AttachmentObjectName,
                    AttachmentFileName = message.AttachmentFileName,
                    AttachmentContentType = message.AttachmentContentType
                },
                cancellationToken);

            DateTime sentAt = DateTime.UtcNow;
            MailDeliveryStatus initialDeliveryStatus = sendResult.DeliveryTrackingEnabled
                ? MailDeliveryStatus.Pending
                : MailDeliveryStatus.NotTracked;
            string? redactedBody = BuildRedactedSensitiveBody(message, transportSucceeded: true);

            transportPersistedAtomically = await repository.MarkTransportSentAsync(
                message.Id,
                sentAt,
                message.AttemptCount,
                message.LastAttemptAt ?? sentAt,
                sendResult.Provider,
                initialDeliveryStatus,
                redactedBody,
                "MailOutboxDispatcher",
                cancellationToken);

            if (!transportPersistedAtomically)
                throw new InvalidOperationException($"Mail outbox message {message.Id} could not be marked as sent.");

            // Keep the in-memory instance aligned for certificate/acceptance side effects.
            message.Status = MailOutboxStatus.Sent;
            message.SentAt = sentAt;
            message.LastError = null;
            message.Provider = sendResult.Provider;
            if (message.LastDeliveryEventAt is null)
                message.DeliveryStatus = initialDeliveryStatus;
            if (redactedBody is not null)
                message.HtmlBody = redactedBody;
        }
        catch (Exception exception)
        {
            message.LastError = exception.Message.Length > 1000
                ? exception.Message[..1000]
                : exception.Message;

            if (message.AttemptCount >= maxAttemptCount)
            {
                message.Status = MailOutboxStatus.Failed;
                RedactSensitiveBodyIfTerminal(message, transportSucceeded: false);
            }

            _logger.LogWarning(
                exception,
                "Mail outbox message could not be sent. MessageId: {MessageId}, Attempt: {AttemptCount}",
                message.Id,
                message.AttemptCount);
        }

        if (!transportPersistedAtomically)
            await repository.UpdateAsync(message);

        await UpdateAcceptanceLetterStatusAsync(acceptanceLetterRepository, message, cancellationToken);
        await UpdateParticipationCertificateStatusAsync(
            participationCertificateRepository,
            submissionFileRepository,
            message,
            certificate,
            cancellationToken);
    }

    private static string? BuildRedactedSensitiveBody(MailOutboxMessage message, bool transportSucceeded)
    {
        if (!message.ContainsSensitiveContent && !message.ParticipationCertificateId.HasValue)
            return null;

        string result = transportSucceeded ? "başarıyla gönderildi" : "gönderilemedi";
        return $"<p>Hassas bağlantı içeren e-posta {result}. Güvenlik nedeniyle e-posta gövdesi arşivlenmedi.</p>";
    }

    private static void RedactSensitiveBodyIfTerminal(MailOutboxMessage message, bool transportSucceeded)
    {
        string? redacted = BuildRedactedSensitiveBody(message, transportSucceeded);
        if (redacted is not null)
            message.HtmlBody = redacted;
    }

    private static async Task UpdateAcceptanceLetterStatusAsync(
        ISubmissionAcceptanceLetterRepository repository,
        MailOutboxMessage message,
        CancellationToken cancellationToken)
    {
        if (!message.AcceptanceLetterId.HasValue || message.Status != MailOutboxStatus.Sent)
            return;

        SubmissionAcceptanceLetter? letter = await repository.GetAsync(
            item => item.Id == message.AcceptanceLetterId.Value,
            cancellationToken: cancellationToken);

        if (letter is null)
            return;

        DateTime now = message.SentAt ?? DateTime.UtcNow;
        letter.SentAt = now;
        letter.SentToEmail = message.ToEmail;
        letter.UpdatedDate = DateTime.UtcNow;
        letter.UpdatedBy = "MailOutboxDispatcher";
        await repository.UpdateAsync(letter);
    }

    private static async Task<ParticipationCertificate?> FindParticipationCertificateAsync(
        IParticipationCertificateRepository repository,
        MailOutboxMessage message,
        CancellationToken cancellationToken)
    {
        if (message.ParticipationCertificateId.HasValue)
        {
            return await repository
                .Query()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    item => item.Id == message.ParticipationCertificateId.Value,
                    cancellationToken);
        }

        // Eski outbox kayıtları için geriye dönük uyumluluk.
        if (string.IsNullOrWhiteSpace(message.AttachmentObjectName))
            return null;

        return await repository
            .Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(item =>
                    item.ObjectName == message.AttachmentObjectName &&
                    (!message.RelatedSubmissionId.HasValue || item.SubmissionId == message.RelatedSubmissionId.Value),
                cancellationToken);
    }

    private static async Task UpdateParticipationCertificateStatusAsync(
        IParticipationCertificateRepository repository,
        ISubmissionFileRepository submissionFileRepository,
        MailOutboxMessage message,
        ParticipationCertificate? certificate,
        CancellationToken cancellationToken)
    {
        certificate ??= await FindParticipationCertificateAsync(repository, message, cancellationToken);
        if (certificate is null || certificate.RevokedAt.HasValue || certificate.DeletedDate.HasValue)
            return;

        DateTime now = DateTime.UtcNow;

        if (message.Status == MailOutboxStatus.Sent)
        {
            DateTime sentAt = message.SentAt ?? now;
            certificate.EmailSentAt = sentAt;
            certificate.PublishedAt = sentAt;
            certificate.EmailStatus = ParticipationCertificateSentFilePendingStatus;
            certificate.EmailError = null;
            certificate.UpdatedDate = now;
            certificate.UpdatedBy = "MailOutboxDispatcher";
            await repository.UpdateAsync(certificate);

            await EnsureParticipationCertificateSubmissionFileAsync(
                submissionFileRepository,
                certificate,
                cancellationToken);

            certificate.EmailStatus = ParticipationCertificateSentStatus;
            certificate.EmailError = null;
        }
        else if (message.Status == MailOutboxStatus.Failed)
        {
            certificate.PublishedAt = null;
            certificate.EmailStatus = ParticipationCertificateFailedStatus;
            certificate.EmailError = message.LastError;
        }
        else if (message.Status == MailOutboxStatus.Cancelled)
        {
            certificate.PublishedAt = null;
            certificate.EmailStatus = "Cancelled";
            certificate.EmailError = message.LastError;
        }
        else
        {
            certificate.PublishedAt = null;
            certificate.EmailStatus = ParticipationCertificateQueuedStatus;
            certificate.EmailError = message.LastError;
        }

        certificate.UpdatedDate = now;
        certificate.UpdatedBy = "MailOutboxDispatcher";
        await repository.UpdateAsync(certificate);
    }


    private static async Task RecoverSentParticipationCertificateStatesAsync(
        IMailOutboxMessageRepository mailRepository,
        IParticipationCertificateRepository certificateRepository,
        ISubmissionFileRepository submissionFileRepository,
        int batchSize,
        CancellationToken cancellationToken)
    {
        List<ParticipationCertificate> candidates = await certificateRepository
            .Query()
            .Where(certificate =>
                certificate.DeletedDate == null &&
                certificate.RevokedAt == null &&
                certificate.EmailSentAt == null &&
                (certificate.EmailStatus == "QueueRequested" ||
                 certificate.EmailStatus == "QueuePreparing" ||
                 certificate.EmailStatus == ParticipationCertificateQueuedStatus))
            .OrderBy(certificate => certificate.EmailQueuedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
            return;

        List<Guid> certificateIds = candidates.Select(certificate => certificate.Id).ToList();
        List<MailOutboxMessage> sentMessages = await mailRepository
            .Query()
            .AsNoTracking()
            .Where(message =>
                message.Status == MailOutboxStatus.Sent &&
                message.ParticipationCertificateId.HasValue &&
                certificateIds.Contains(message.ParticipationCertificateId.Value))
            .OrderByDescending(message => message.SentAt)
            .ToListAsync(cancellationToken);

        Dictionary<Guid, MailOutboxMessage> sentMap = sentMessages
            .GroupBy(message => message.ParticipationCertificateId!.Value)
            .ToDictionary(group => group.Key, group => group.First());

        foreach (ParticipationCertificate certificate in candidates)
        {
            if (!sentMap.TryGetValue(certificate.Id, out MailOutboxMessage? sentMessage))
                continue;

            DateTime sentAt = sentMessage.SentAt ?? DateTime.UtcNow;
            certificate.EmailSentAt = sentAt;
            certificate.PublishedAt = sentAt;
            certificate.EmailStatus = ParticipationCertificateSentFilePendingStatus;
            certificate.EmailError = null;
            certificate.UpdatedDate = DateTime.UtcNow;
            certificate.UpdatedBy = "MailOutboxDispatcherSentRecovery";
            await certificateRepository.UpdateAsync(certificate);

            await EnsureParticipationCertificateSubmissionFileAsync(
                submissionFileRepository,
                certificate,
                cancellationToken);

            certificate.EmailStatus = ParticipationCertificateSentStatus;
            certificate.UpdatedDate = DateTime.UtcNow;
            certificate.UpdatedBy = "MailOutboxDispatcherSentRecovery";
            await certificateRepository.UpdateAsync(certificate);
        }
    }

    private static async Task PublishPendingParticipationCertificateFilesAsync(
        IParticipationCertificateRepository participationCertificateRepository,
        ISubmissionFileRepository submissionFileRepository,
        int batchSize,
        CancellationToken cancellationToken)
    {
        List<ParticipationCertificate> certificates = await participationCertificateRepository
            .Query()
            .Where(certificate =>
                certificate.DeletedDate == null &&
                certificate.RevokedAt == null &&
                certificate.EmailSentAt != null &&
                certificate.EmailStatus == ParticipationCertificateSentFilePendingStatus)
            .OrderBy(certificate => certificate.EmailSentAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (ParticipationCertificate certificate in certificates)
        {
            await EnsureParticipationCertificateSubmissionFileAsync(
                submissionFileRepository,
                certificate,
                cancellationToken);

            certificate.PublishedAt ??= certificate.EmailSentAt ?? DateTime.UtcNow;
            certificate.EmailStatus = ParticipationCertificateSentStatus;
            certificate.EmailError = null;
            certificate.UpdatedDate = DateTime.UtcNow;
            certificate.UpdatedBy = "MailOutboxDispatcherFileRecovery";
            await participationCertificateRepository.UpdateAsync(certificate);
        }
    }

    private static async Task EnsureParticipationCertificateSubmissionFileAsync(
        ISubmissionFileRepository repository,
        ParticipationCertificate certificate,
        CancellationToken cancellationToken)
    {
        if (certificate.SubmissionId == Guid.Empty || string.IsNullOrWhiteSpace(certificate.ObjectName))
            return;

        SubmissionFile? file = await repository
            .Query()
            .IgnoreQueryFilters()
            .Where(item =>
                item.SubmissionId == certificate.SubmissionId &&
                item.FileKind == SubmissionFileKind.ParticipationCertificate &&
                item.FilePath == certificate.ObjectName)
            .OrderByDescending(item => item.IsActive)
            .ThenByDescending(item => item.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);

        DateTime publishedAt = certificate.PublishedAt ?? certificate.EmailSentAt ?? DateTime.UtcNow;

        if (file is null)
        {
            file = new SubmissionFile
            {
                Id = Guid.NewGuid(),
                SubmissionId = certificate.SubmissionId,
                FileKind = SubmissionFileKind.ParticipationCertificate,
                OriginalFileName = certificate.FileName,
                FilePath = certificate.ObjectName,
                ContentType = string.IsNullOrWhiteSpace(certificate.ContentType)
                    ? "application/pdf"
                    : certificate.ContentType,
                FileSize = certificate.FileSize,
                ReviewStatus = SubmissionFileReviewStatus.Approved,
                ReviewedAt = publishedAt,
                IsIncludedInProgramBook = false,
                VersionNo = 1,
                IsActive = true,
                CreatedDate = publishedAt,
                CreatedBy = "MailOutboxDispatcher"
            };

            await repository.AddAsync(file);
            return;
        }

        bool requiresUpdate =
            !file.IsActive ||
            file.DeletedDate.HasValue ||
            !string.Equals(file.OriginalFileName, certificate.FileName, StringComparison.Ordinal) ||
            !string.Equals(file.ContentType, certificate.ContentType, StringComparison.OrdinalIgnoreCase) ||
            file.FileSize != certificate.FileSize;

        if (!requiresUpdate)
            return;

        file.OriginalFileName = certificate.FileName;
        file.FilePath = certificate.ObjectName;
        file.ContentType = string.IsNullOrWhiteSpace(certificate.ContentType)
            ? "application/pdf"
            : certificate.ContentType;
        file.FileSize = certificate.FileSize;
        file.ReviewStatus = SubmissionFileReviewStatus.Approved;
        file.ReviewedAt = publishedAt;
        file.IsIncludedInProgramBook = false;
        file.VersionNo = Math.Max(1, file.VersionNo);
        file.IsActive = true;
        file.DeletedDate = null;
        file.DeletedBy = null;
        file.UpdatedDate = publishedAt;
        file.UpdatedBy = "MailOutboxDispatcher";
        await repository.UpdateAsync(file);
    }
}
