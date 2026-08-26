using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Domain.Workflow;

public sealed class WorkflowTransitionEffect : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public int TransactionStatusTransitionId { get; set; }

    public WorkflowEffectType EffectType { get; set; }

    public string ParametersJson { get; set; } = "{}";

    public int Order { get; set; }

    public bool IsActive { get; set; } = true;

    public TransactionStatusTransition TransactionStatusTransition { get; set; } = null!;
}
