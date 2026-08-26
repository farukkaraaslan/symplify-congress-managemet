using Symplify.BackOffice.Application.Features.MailDeliveries.Dtos;

namespace Symplify.BackOffice.Infrastructure.Email.Ses;

public interface IAmazonSesSnsAdapter
{
    Task<AmazonSnsEnvelope> ParseAndValidateAsync(
        string rawJson,
        CancellationToken cancellationToken = default);

    Task ConfirmSubscriptionAsync(
        AmazonSnsEnvelope envelope,
        CancellationToken cancellationToken = default);

    MailDeliveryProviderEventDto? ParseSesEvent(AmazonSnsEnvelope envelope);
}
