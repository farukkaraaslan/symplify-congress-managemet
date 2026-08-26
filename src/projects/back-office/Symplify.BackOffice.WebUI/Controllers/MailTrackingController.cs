using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.Application.Features.BulkEmails.Commands.TrackOpen;

namespace Symplify.BackOffice.WebUI.Controllers;

[AllowAnonymous]
[Route("mail-tracking")]
public sealed class MailTrackingController : Controller
{
    private static readonly byte[] TransparentGif = Convert.FromBase64String(
        "R0lGODlhAQABAIAAAAAAAP///ywAAAAAAQABAAACAUwAOw==");

    private readonly IMediator _mediator;
    private readonly ILogger<MailTrackingController> _logger;

    public MailTrackingController(
        IMediator mediator,
        ILogger<MailTrackingController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet("open/{token:guid}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Open(Guid token, CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-store, no-cache, max-age=0, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive";

        if (token != Guid.Empty)
        {
            try
            {
                await _mediator.Send(
                    new TrackBulkEmailOpenCommand { TrackingToken = token },
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                // Tracking must never break image delivery or reveal whether the token exists.
                _logger.LogDebug(exception, "Bulk email open tracking could not be recorded.");
            }
        }

        return File(TransparentGif, "image/gif");
    }
}
