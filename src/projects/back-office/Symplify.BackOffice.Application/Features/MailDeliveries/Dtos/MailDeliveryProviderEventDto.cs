using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.MailDeliveries.Dtos;

/// <summary>
/// Provider-neutral event produced by the Amazon SES/SNS adapter.
/// No AWS-specific JSON parsing belongs in the Application layer.
/// </summary>
public sealed class MailDeliveryProviderEventDto
{
    public Guid MailOutboxMessageId { get; set; }

    public string ProviderEventId { get; set; } = string.Empty;

    public string? ProviderMessageId { get; set; }

    public MailDeliveryEventType EventType { get; set; }

    public DateTime OccurredAt { get; set; }

    public string? DestinationEmail { get; set; }

    public string? StatusCode { get; set; }

    public string? DiagnosticCode { get; set; }

    public string? BounceType { get; set; }

    public string? BounceSubType { get; set; }

    public string? SmtpResponse { get; set; }

    public string? Detail { get; set; }
}
