namespace Symplify.BackOffice.Application.Features.SubmissionReviewers.Commands.Assign;

public sealed class AssignedReviewerToSubmissionResponse
{
    public Guid SubmissionId { get; set; }
    public Guid ReviewerId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
}
