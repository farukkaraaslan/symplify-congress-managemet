using Symplify.BackOffice.Application.Services.Workflow;

namespace Symplify.BackOffice.Application.Features.SubmissionWorkflow.Queries.GetAllowedTransitions;

public sealed class GetAllowedSubmissionTransitionsResponse
{
    public Guid SubmissionId { get; set; }

    public List<AllowedWorkflowTransitionDto> Transitions { get; set; } = new();
}
