using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Features.MailDeliveries.Commands.ProcessProviderEvent;
using Symplify.BackOffice.Application.Features.MailDeliveries.Dtos;
using Symplify.BackOffice.Infrastructure.Email;
using Symplify.BackOffice.Infrastructure.Email.Ses;

namespace Symplify.BackOffice.WebUI.Controllers;

/// <summary>
/// Public integration endpoint used only by the configured Amazon SNS topic.
/// Authentication is Amazon SNS signature + TopicArn validation, not a user cookie.
/// </summary>
[ApiController]
[Route("integrations/mail-delivery/amazon-ses/sns")]
public sealed class MailDeliveryIntegrationController : ControllerBase
{
    private readonly IAmazonSesSnsAdapter _adapter;
    private readonly IMediator _mediator;
    private readonly BackOfficeMailOptions _mailOptions;
    private readonly ILogger<MailDeliveryIntegrationController> _logger;

    public MailDeliveryIntegrationController(
        IAmazonSesSnsAdapter adapter,
        IMediator mediator,
        IOptions<BackOfficeMailOptions> mailOptions,
        ILogger<MailDeliveryIntegrationController> logger)
    {
        _adapter = adapter;
        _mediator = mediator;
        _mailOptions = mailOptions.Value;
        _logger = logger;
    }

    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [HttpPost]
    public async Task<IActionResult> AmazonSesSns(CancellationToken cancellationToken)
    {
        if (!_mailOptions.SesTracking.Enabled)
            return NotFound();

        if (Request.ContentLength is > 524288)
            return BadRequest();

        string rawBody;
        using (StreamReader reader = new(Request.Body))
            rawBody = await reader.ReadToEndAsync(cancellationToken);

        AmazonSnsEnvelope envelope;
        try
        {
            envelope = await _adapter.ParseAndValidateAsync(rawBody, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Rejected invalid Amazon SNS mail-delivery webhook.");
            return BadRequest();
        }

        if (string.Equals(envelope.Type, "SubscriptionConfirmation", StringComparison.Ordinal))
        {
            await _adapter.ConfirmSubscriptionAsync(envelope, cancellationToken);
            return Ok();
        }

        if (!string.Equals(envelope.Type, "Notification", StringComparison.Ordinal))
            return Ok();

        MailDeliveryProviderEventDto? providerEvent = _adapter.ParseSesEvent(envelope);
        if (providerEvent is null)
        {
            _logger.LogDebug(
                "Amazon SES event ignored because it is unsupported or has no Symplify correlation tag. SNS MessageId: {MessageId}",
                envelope.MessageId);
            return Ok();
        }

        bool applied = await _mediator.Send(
            new ProcessMailDeliveryProviderEventCommand
            {
                Event = providerEvent
            },
            cancellationToken);

        if (!applied)
        {
            _logger.LogWarning(
                "Amazon SES event could not be correlated. SNS MessageId: {MessageId}, OutboxId: {OutboxId}",
                envelope.MessageId,
                providerEvent.MailOutboxMessageId);
        }

        // Return 200 after signature validation even when a historical/unknown row cannot be correlated;
        // otherwise SNS retries a poison event indefinitely.
        return Ok();
    }
}
