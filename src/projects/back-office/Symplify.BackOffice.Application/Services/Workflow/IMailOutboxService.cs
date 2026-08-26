using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Application.Services.Workflow;

public interface IMailOutboxService
{
    Task QueueAcceptanceEmailAsync(
        Symplify.BackOffice.Domain.Submission.Submission submission,
        SubmissionAcceptanceLetter acceptanceLetter,
        string? toEmail,
        CancellationToken cancellationToken);

    Task QueueSubmissionStatusEmailAsync(
        Symplify.BackOffice.Domain.Submission.Submission submission,
        string templateCode,
        string? toEmail,
        CancellationToken cancellationToken);

    Task QueuePaymentPendingEmailAsync(
        Symplify.BackOffice.Domain.Submission.Submission submission,
        string? toEmail,
        CancellationToken cancellationToken);

    Task QueuePaymentApprovedEmailAsync(
        Symplify.BackOffice.Domain.Submission.Submission submission,
        string? toEmail,
        CancellationToken cancellationToken);
}
