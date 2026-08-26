namespace Symplify.BackOffice.Application.Services.Workflow;

public interface IWorkflowEngine
{
    Task<IReadOnlyCollection<AllowedWorkflowTransitionDto>> GetAllowedTransitionsAsync(
        Guid submissionId,
        Guid? performedByUserId,
        CancellationToken cancellationToken);

    Task<ChangeWorkflowStatusResult> ChangeStatusAsync(
        Guid submissionId,
        int transitionId,
        Guid? performedByUserId,
        string? publicNote,
        string? internalNote,
        CancellationToken cancellationToken);

    Task<ChangeWorkflowStatusResult> ChangeStatusByCodeAsync(
        Guid submissionId,
        string targetStatusCode,
        Guid? performedByUserId,
        string? publicNote,
        string? internalNote,
        CancellationToken cancellationToken);

    Task<ChangeWorkflowStatusResult> ExecuteNextAutomaticTransitionAsync(
        Guid submissionId,
        Guid? performedByUserId,
        string? publicNote,
        string? internalNote,
        CancellationToken cancellationToken);

    Task<ChangeWorkflowStatusResult> ExecuteAutomaticTransitionToStatusAsync(
        Guid submissionId,
        string targetStatusCode,
        Guid? performedByUserId,
        string? publicNote,
        string? internalNote,
        CancellationToken cancellationToken);
}
