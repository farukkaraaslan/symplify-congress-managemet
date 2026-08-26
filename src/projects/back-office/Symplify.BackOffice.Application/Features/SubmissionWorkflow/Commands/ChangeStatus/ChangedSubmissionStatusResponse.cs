namespace Symplify.BackOffice.Application.Features.SubmissionWorkflow.Commands.ChangeStatus;

public sealed class ChangedSubmissionStatusResponse
{
    public bool Success { get; set; }

    public string? Message { get; set; }

    public Guid SubmissionId { get; set; }

    public int? NewStatusId { get; set; }
}
