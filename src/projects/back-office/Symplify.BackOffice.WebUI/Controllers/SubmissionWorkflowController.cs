using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.Application.Features.SubmissionWorkflow.Commands.ChangeStatus;
using Symplify.BackOffice.Application.Features.SubmissionWorkflow.Commands.CompletePayment;
using Symplify.BackOffice.Application.Features.SubmissionWorkflow.Commands.Reject;
using Symplify.BackOffice.Application.Features.SubmissionWorkflow.Commands.RevertPayment;
using Symplify.BackOffice.Application.Features.SubmissionWorkflow.Commands.RestartRejectedProcess;
using Symplify.BackOffice.Application.Features.SubmissionWorkflow.Queries.GetAllowedTransitions;
using Symplify.BackOffice.Application.Features.Submissions.Constants;
using Symplify.BackOffice.WebUI.Localization;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture?}/submissions/workflow")]
public sealed class SubmissionWorkflowController : Controller
{
    private readonly IMediator _mediator;
    private readonly IBackOfficeViewLocalizer _localizer;

    public SubmissionWorkflowController(IMediator mediator, IBackOfficeViewLocalizer localizer)
    {
        _mediator = mediator;
        _localizer = localizer;
    }

    [HttpGet("allowed/{submissionId:guid}")]
    public async Task<IActionResult> Allowed(Guid submissionId, CancellationToken cancellationToken)
    {
        GetAllowedSubmissionTransitionsResponse response = await _mediator.Send(new GetAllowedSubmissionTransitionsQuery
        {
            SubmissionId = submissionId,
            PerformedByUserId = GetCurrentUserId()
        }, cancellationToken);

        return Json(response.Transitions);
    }

    [HttpPost("change-status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(ChangeSubmissionStatusCommand command, CancellationToken cancellationToken)
    {
        command.PerformedByUserId ??= GetCurrentUserId();

        ChangedSubmissionStatusResponse response = await _mediator.Send(command, cancellationToken);
        if (!response.Success)
        {
            TempData["ErrorMessage"] = response.Message ?? "İşlem gerçekleştirilemedi.";
            return RedirectToManage(command.SubmissionId);
        }

        TempData["SuccessMessage"] = "Bildiri süreci güncellendi.";
        return RedirectToManage(command.SubmissionId);
    }



    [HttpPost("reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(RejectSubmissionCommand command, CancellationToken cancellationToken)
    {
        command.PerformedByUserId ??= GetCurrentUserId();

        RejectSubmissionResponse response = await _mediator.Send(command, cancellationToken);
        if (!response.Success)
        {
            TempData["ErrorMessage"] = response.Message ?? "Bildiri reddedilemedi.";
            return RedirectToManage(command.SubmissionId);
        }

        TempData["SuccessMessage"] = "Bildiri reddedildi. Yazar erişimi kapatıldı.";
        return RedirectToManage(command.SubmissionId);
    }

    [HttpPost("restart-rejected-process")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestartRejectedProcess(RestartRejectedSubmissionProcessCommand command, CancellationToken cancellationToken)
    {
        command.PerformedByUserId ??= GetCurrentUserId();

        RestartRejectedSubmissionProcessResponse response = await _mediator.Send(command, cancellationToken);
        if (!response.Success)
        {
            TempData["ErrorMessage"] = response.Message ?? "Bildiri süreci yeniden başlatılamadı.";
            return RedirectToManage(command.SubmissionId);
        }

        TempData["SuccessMessage"] = "Bildiri süreci yeniden başlatıldı.";
        return RedirectToManage(command.SubmissionId);
    }

    [HttpPost("complete-payment")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompletePayment(CompleteSubmissionPaymentCommand command, CancellationToken cancellationToken)
    {
        command.PerformedByUserId ??= GetCurrentUserId();

        CompletedSubmissionPaymentResponse response = await _mediator.Send(command, cancellationToken);
        if (!response.Success)
        {
            TempData["ErrorMessage"] = response.Message ?? "Ödeme işlemi tamamlanamadı.";
            return RedirectToManage(command.SubmissionId);
        }

        TempData["SuccessMessage"] = "Ödeme işlemi tamamlandı.";
        return RedirectToManage(command.SubmissionId);
    }

    [HttpPost("revert-payment")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevertPayment(RevertSubmissionPaymentCommand command, CancellationToken cancellationToken)
    {
        command.PerformedByUserId ??= GetCurrentUserId();

        RevertedSubmissionPaymentResponse response = await _mediator.Send(command, cancellationToken);
        if (!response.Success)
        {
            TempData["ErrorMessage"] = response.Message ?? _localizer.GetStringValue(SubmissionManagementResourceKeys.PaymentRevertErrorMessage);
            return RedirectToManage(command.SubmissionId);
        }

        TempData["SuccessMessage"] = _localizer.GetStringValue(SubmissionManagementResourceKeys.PaymentRevertSuccessMessage);
        return RedirectToManage(command.SubmissionId);
    }

    private Guid? GetCurrentUserId()
    {
        string? value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out Guid userId) ? userId : null;
    }

    private RedirectResult RedirectToManage(Guid submissionId)
    {
        string culture = RouteData.Values["culture"]?.ToString();
        if (string.IsNullOrWhiteSpace(culture))
            culture = "tr-TR";

        return Redirect($"/{culture}/submission-management/{submissionId:D}");
    }
}
