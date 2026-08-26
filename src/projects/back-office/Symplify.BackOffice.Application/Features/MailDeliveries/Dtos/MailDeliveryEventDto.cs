using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.MailDeliveries.Dtos;

public sealed class MailDeliveryEventDto
{
    public Guid Id { get; set; }
    public MailDeliveryEventType EventType { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? StatusCode { get; set; }
    public string? DiagnosticCode { get; set; }
    public string? BounceType { get; set; }
    public string? BounceSubType { get; set; }
    public string? SmtpResponse { get; set; }
    public string? Detail { get; set; }
}
