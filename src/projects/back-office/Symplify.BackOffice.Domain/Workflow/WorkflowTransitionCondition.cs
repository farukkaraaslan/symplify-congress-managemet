using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Domain.Workflow;

public sealed class WorkflowTransitionCondition : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public int TransactionStatusTransitionId { get; set; }

    public WorkflowConditionSubject Subject { get; set; }

    public WorkflowConditionField Field { get; set; }

    public WorkflowConditionOperator Operator { get; set; }

    public string? ExpectedValue { get; set; }

    public string? ExpectedValueSource { get; set; }

    public string? FailureMessageResourceKey { get; set; }

    public int Order { get; set; }

    public bool IsActive { get; set; } = true;

    public TransactionStatusTransition TransactionStatusTransition { get; set; } = null!;
}
