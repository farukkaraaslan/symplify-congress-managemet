using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Services.Workflow;

public interface IWorkflowEffectProcessor
{
    Task ProcessAsync(
        WorkflowContext context,
        IReadOnlyCollection<WorkflowTransitionEffect> effects,
        CancellationToken cancellationToken);
}
