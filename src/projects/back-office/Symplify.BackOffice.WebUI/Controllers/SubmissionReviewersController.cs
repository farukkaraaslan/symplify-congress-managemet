using System.Security.Claims;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.Application.Features.SubmissionReviewers.Commands.Assign;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture?}/submission-reviewers")]
public sealed class SubmissionReviewersController : Controller
{
    private readonly IMediator _mediator;

    public SubmissionReviewersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("assign")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(AssignReviewerToSubmissionCommand command, CancellationToken cancellationToken)
    {
        if (command.SubmissionId == Guid.Empty || command.ReviewerId == Guid.Empty)
        {
            TempData["ErrorMessage"] = "Bildiri ve hakem seçimi zorunludur.";
            return RedirectToSubmission(command.SubmissionId);
        }

        command.PerformedByUserId = GetCurrentUserId();

        try
        {
            AssignedReviewerToSubmissionResponse response = await _mediator.Send(command, cancellationToken);

            TempData["SuccessMessage"] = $"{response.ReviewerName} hakem olarak atandı.";
            return RedirectToSubmission(response.SubmissionId);
        }
        catch (BusinessException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
            return RedirectToSubmission(command.SubmissionId);
        }
    }

    private RedirectToActionResult RedirectToSubmission(Guid submissionId)
    {
        string? culture = RouteData.Values["culture"]?.ToString();
        return RedirectToAction("Manage", "SubmissionManagement", new { culture, id = submissionId });
    }

    private Guid? GetCurrentUserId()
    {
        string? rawId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(rawId, out Guid userId) ? userId : null;
    }
}
