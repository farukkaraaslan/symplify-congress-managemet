using Symplify.BackOffice.Application.Services.Mailing;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Communication;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Services.Email;

public sealed class ApplicationMailQueueService : IApplicationMailQueueService
{
    private readonly IMailOutboxMessageRepository _repository;
    private readonly IOrganizationMailConfigurationResolver _mailConfigurationResolver;

    public ApplicationMailQueueService(
        IMailOutboxMessageRepository repository,
        IOrganizationMailConfigurationResolver mailConfigurationResolver)
    {
        _repository = repository;
        _mailConfigurationResolver = mailConfigurationResolver;
    }

    public async Task<MailOutboxMessage> QueueAsync(
        MailQueueRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Message);

        BackOfficeEmailMessage email = request.Message;
        if (email.OrganizationId == Guid.Empty)
            throw new InvalidOperationException("OrganizationId is required for every outgoing email.");

        string toEmail = NormalizeRequired(email.ToEmail, nameof(email.ToEmail), 250);
        string subject = NormalizeRequired(email.Subject, nameof(email.Subject), 300);

        ResolvedOrganizationMailConfiguration sender = await _mailConfigurationResolver.ResolveAsync(
            email.OrganizationId,
            cancellationToken);

        DateTime now = DateTime.UtcNow;
        MailOutboxMessage entity = new()
        {
            Id = Guid.NewGuid(),
            MailType = request.MailType,
            OrganizationId = email.OrganizationId,
            CongressId = email.CongressId,
            RelatedUserId = request.RelatedUserId,
            RelatedAuthorId = request.RelatedAuthorId,
            RelatedSubmissionId = request.RelatedSubmissionId,
            AcceptanceLetterId = request.AcceptanceLetterId,
            ParticipationCertificateId = request.ParticipationCertificateId,
            BulkEmailBatchId = request.BulkEmailBatchId,
            BulkEmailAudienceType = request.BulkEmailAudienceType,
            BulkEmailCulture = NormalizeOptional(request.BulkEmailCulture, 15),
            TrackingToken = request.TrackingToken,
            ToEmail = toEmail,
            ToName = NormalizeOptional(email.ToName, 250),
            Subject = subject,
            HtmlBody = email.HtmlBody ?? string.Empty,
            FromEmail = NormalizeOptional(FirstNotEmpty(email.FromEmail, sender.FromEmail), 250),
            FromName = NormalizeOptional(FirstNotEmpty(email.FromName, sender.FromName), 250),
            ReplyToEmail = NormalizeOptional(FirstNotEmpty(email.ReplyToEmail, sender.ReplyToEmail), 250),
            ReplyToName = NormalizeOptional(FirstNotEmpty(email.ReplyToName, sender.ReplyToName), 250),
            AttachmentPath = NormalizeOptional(email.AttachmentPath, 750),
            AttachmentBucketName = NormalizeOptional(email.AttachmentBucketName, 150),
            AttachmentObjectName = NormalizeOptional(email.AttachmentObjectName, 750),
            AttachmentFileName = NormalizeOptional(email.AttachmentFileName, 260),
            AttachmentContentType = NormalizeOptional(email.AttachmentContentType, 150),
            Status = request.ImmediateDispatch ? MailOutboxStatus.Processing : MailOutboxStatus.Pending,
            DeliveryStatus = MailDeliveryStatus.Unknown,
            ContainsSensitiveContent = request.ContainsSensitiveContent,
            CreatedDate = now,
            CreatedBy = NormalizeOptional(request.CreatedBy, 250) ?? "ApplicationMailQueue"
        };

        return await _repository.AddAsync(entity);
    }

    private static string NormalizeRequired(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{fieldName} is required for every outgoing email.");

        string normalized = value.Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();

        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        string normalized = value.Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();

        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static string? FirstNotEmpty(string? primary, string? fallback)
        => !string.IsNullOrWhiteSpace(primary) ? primary : fallback;
}
