using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Symplify.BackOffice.Application.Features.MailDeliveries.Dtos;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Communication;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.MailDeliveries.Commands.ProcessProviderEvent;

/// <summary>
/// Applies a verified provider event. This command intentionally has no user permission behavior:
/// the public integration endpoint is authenticated by Amazon SNS signature validation before dispatch.
/// </summary>
public sealed class ProcessMailDeliveryProviderEventCommand : IRequest<bool>
{
    public MailDeliveryProviderEventDto Event { get; set; } = new();

    public sealed class Handler : IRequestHandler<ProcessMailDeliveryProviderEventCommand, bool>
    {
        private readonly IMailOutboxMessageRepository _outboxRepository;
        private readonly IMailDeliveryEventRepository _eventRepository;
        private readonly ILogger<Handler> _logger;

        public Handler(
            IMailOutboxMessageRepository outboxRepository,
            IMailDeliveryEventRepository eventRepository,
            ILogger<Handler> logger)
        {
            _outboxRepository = outboxRepository;
            _eventRepository = eventRepository;
            _logger = logger;
        }

        public async Task<bool> Handle(
            ProcessMailDeliveryProviderEventCommand request,
            CancellationToken cancellationToken)
        {
            MailDeliveryProviderEventDto providerEvent = request.Event;
            if (providerEvent.MailOutboxMessageId == Guid.Empty ||
                string.IsNullOrWhiteSpace(providerEvent.ProviderEventId))
            {
                return false;
            }

            bool duplicate = await _eventRepository.AnyAsync(
                item => item.ProviderEventId == providerEvent.ProviderEventId,
                enableTracking: false,
                cancellationToken: cancellationToken);

            if (duplicate)
                return true;

            MailOutboxMessage? message = await _outboxRepository.GetAsync(
                item => item.Id == providerEvent.MailOutboxMessageId,
                cancellationToken: cancellationToken);

            if (message is null)
            {
                _logger.LogWarning(
                    "SES delivery event references an unknown outbox message. OutboxId: {OutboxId}, ProviderEventId: {ProviderEventId}",
                    providerEvent.MailOutboxMessageId,
                    providerEvent.ProviderEventId);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(providerEvent.DestinationEmail) &&
                !string.Equals(
                    message.ToEmail.Trim(),
                    providerEvent.DestinationEmail.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "SES delivery event destination does not match the outbox recipient. OutboxId: {OutboxId}",
                    message.Id);
                return false;
            }

            DateTime occurredAt = EnsureUtc(providerEvent.OccurredAt);
            DateTime now = DateTime.UtcNow;

            MailDeliveryEvent entity = new()
            {
                Id = Guid.NewGuid(),
                MailOutboxMessageId = message.Id,
                ProviderEventId = TruncateRequired(providerEvent.ProviderEventId, 200),
                ProviderMessageId = Truncate(providerEvent.ProviderMessageId, 200),
                EventType = providerEvent.EventType,
                OccurredAt = occurredAt,
                StatusCode = Truncate(providerEvent.StatusCode, 100),
                DiagnosticCode = Truncate(providerEvent.DiagnosticCode, 2000),
                BounceType = Truncate(providerEvent.BounceType, 100),
                BounceSubType = Truncate(providerEvent.BounceSubType, 100),
                SmtpResponse = Truncate(providerEvent.SmtpResponse, 2000),
                Detail = Truncate(providerEvent.Detail, 2000),
                CreatedDate = now,
                CreatedBy = "AmazonSES/SNS"
            };

            await _eventRepository.AddAsync(entity);

            // SNS can occasionally deliver events out of order. Keep all history, but only let a newer
            // event replace the list-summary state.
            if (message.LastDeliveryEventAt.HasValue && occurredAt < message.LastDeliveryEventAt.Value)
                return true;

            message.Provider = "AmazonSES";
            message.ProviderMessageId = Truncate(providerEvent.ProviderMessageId, 200) ?? message.ProviderMessageId;
            message.LastDeliveryEventAt = occurredAt;
            message.DeliveryStatusCode = Truncate(providerEvent.StatusCode, 100);
            message.DeliveryDiagnosticCode = Truncate(providerEvent.DiagnosticCode, 2000);
            message.DeliverySmtpResponse = Truncate(providerEvent.SmtpResponse, 2000);
            message.BounceType = Truncate(providerEvent.BounceType, 100);
            message.BounceSubType = Truncate(providerEvent.BounceSubType, 100);
            message.UpdatedDate = now;
            message.UpdatedBy = "AmazonSES/SNS";

            ApplySummary(message, providerEvent.EventType, occurredAt);
            await _outboxRepository.UpdateAsync(message);

            return true;
        }

        private static void ApplySummary(
            MailOutboxMessage message,
            MailDeliveryEventType eventType,
            DateTime occurredAt)
        {
            switch (eventType)
            {
                case MailDeliveryEventType.Send:
                    // SEND means SES accepted the message for delivery; it is not recipient delivery proof.
                    if (message.DeliveryStatus is MailDeliveryStatus.Unknown or MailDeliveryStatus.NotTracked)
                        message.DeliveryStatus = MailDeliveryStatus.Pending;
                    break;

                case MailDeliveryEventType.Delivery:
                    message.DeliveryStatus = MailDeliveryStatus.Delivered;
                    message.DeliveredAt = occurredAt;
                    break;

                case MailDeliveryEventType.DeliveryDelay:
                    message.DeliveryStatus = MailDeliveryStatus.Delayed;
                    break;

                case MailDeliveryEventType.Bounce:
                    message.DeliveryStatus = MailDeliveryStatus.Bounced;
                    message.BouncedAt = occurredAt;
                    break;

                case MailDeliveryEventType.Reject:
                    message.DeliveryStatus = MailDeliveryStatus.Rejected;
                    break;

                case MailDeliveryEventType.Complaint:
                    message.DeliveryStatus = MailDeliveryStatus.Complaint;
                    message.ComplainedAt = occurredAt;
                    break;

                case MailDeliveryEventType.RenderingFailure:
                    message.DeliveryStatus = MailDeliveryStatus.RenderingFailed;
                    break;
            }
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            if (value == default)
                return DateTime.UtcNow;

            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }

        private static string? Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            string normalized = value.Trim();
            return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
        }

        private static string TruncateRequired(string value, int maxLength)
        {
            string normalized = value.Trim();
            return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
        }
    }
}
