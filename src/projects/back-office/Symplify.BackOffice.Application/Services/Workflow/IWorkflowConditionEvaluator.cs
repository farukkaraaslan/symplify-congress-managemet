using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Services.Workflow;

public interface IWorkflowConditionEvaluator
{
    Task<WorkflowConditionEvaluationResult> EvaluateAsync(
        WorkflowContext context,
        IReadOnlyCollection<WorkflowTransitionCondition> conditions,
        CancellationToken cancellationToken);
}
