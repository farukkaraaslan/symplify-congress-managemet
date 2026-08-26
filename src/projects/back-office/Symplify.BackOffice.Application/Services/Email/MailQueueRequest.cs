using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Services.Email;

/// <summary>
/// Provider-independent metadata used to create the master outgoing-mail record.
/// </summary>
public sealed class MailQueueRequest
{
    public BackOfficeEmailMessage Message { get; set; } = new();

    public MailMessageType MailType { get; set; } = MailMessageType.Unknown;

    public Guid? RelatedUserId { get; set; }

    public Guid? RelatedAuthorId { get; set; }

    public Guid? RelatedSubmissionId { get; set; }

    public Guid? AcceptanceLetterId { get; set; }

    public Guid? ParticipationCertificateId { get; set; }

    public Guid? BulkEmailBatchId { get; set; }

    public BulkEmailAudienceType? BulkEmailAudienceType { get; set; }

    public string? BulkEmailCulture { get; set; }

    public Guid? TrackingToken { get; set; }

    public bool ContainsSensitiveContent { get; set; }

    public bool ImmediateDispatch { get; set; }

    public string? CreatedBy { get; set; }
}
