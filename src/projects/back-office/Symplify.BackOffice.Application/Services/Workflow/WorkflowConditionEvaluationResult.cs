namespace Symplify.BackOffice.Application.Services.Workflow;

public sealed class WorkflowConditionEvaluationResult
{
    public bool IsAllowed { get; init; }
    public string? FailureMessage { get; init; }

    public static WorkflowConditionEvaluationResult Allowed() => new() { IsAllowed = true };

    public static WorkflowConditionEvaluationResult Denied(string? message) => new()
    {
        IsAllowed = false,
        FailureMessage = string.IsNullOrWhiteSpace(message) ? "Workflow condition failed." : message
    };
}
