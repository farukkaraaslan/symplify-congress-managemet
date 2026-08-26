using Symplify.BackOffice.Application.Features.MailDeliveries.Dtos;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.MailDeliveries.Queries.GetDetail;

public sealed class GetMailDeliveryDetailResponse
{
    public Guid Id { get; set; }
    public MailMessageType MailType { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public Guid? OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public Guid? CongressId { get; set; }
    public string CongressName { get; set; } = string.Empty;
    public Guid? RelatedUserId { get; set; }
    public Guid? RelatedAuthorId { get; set; }
    public Guid? RelatedSubmissionId { get; set; }
    public string? SubmissionNumber { get; set; }
    public Guid? AcceptanceLetterId { get; set; }
    public string? AcceptanceLetterNumber { get; set; }
    public Guid? ParticipationCertificateId { get; set; }
    public string? ParticipationCertificateFileName { get; set; }
    public Guid? BulkEmailBatchId { get; set; }
    public MailOutboxStatus Status { get; set; }
    public MailDeliveryStatus DeliveryStatus { get; set; }
    public int AttemptCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? BouncedAt { get; set; }
    public DateTime? ComplainedAt { get; set; }
    public DateTime? LastDeliveryEventAt { get; set; }
    public string? Provider { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? LastError { get; set; }
    public string? DeliveryStatusCode { get; set; }
    public string? DeliveryDiagnosticCode { get; set; }
    public string? DeliverySmtpResponse { get; set; }
    public string? BounceType { get; set; }
    public string? BounceSubType { get; set; }
    public IReadOnlyList<MailDeliveryEventDto> Events { get; set; } = Array.Empty<MailDeliveryEventDto>();
}
