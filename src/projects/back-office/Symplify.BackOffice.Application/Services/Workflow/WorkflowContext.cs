using Symplify.BackOffice.Domain.Submission;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Services.Workflow;

public sealed class WorkflowContext
{
    public Submission Submission { get; init; } = null!;
    public TransactionStatusTransition? Transition { get; init; }
    public Guid? PerformedByUserId { get; init; }
    public string? PublicNote { get; init; }
    public string? InternalNote { get; init; }
}
