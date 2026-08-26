using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.MailDeliveries.Dtos;

public sealed class MailDeliveryListItemDto
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
    public Guid? RelatedSubmissionId { get; set; }
    public string? SubmissionNumber { get; set; }
    public MailOutboxStatus Status { get; set; }
    public MailDeliveryStatus DeliveryStatus { get; set; }
    public string? Provider { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? LastError { get; set; }
    public string? DeliveryDiagnosticCode { get; set; }
}
