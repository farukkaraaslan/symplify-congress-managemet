using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressEvaluationCriterion : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressId { get; set; }
    public Guid EvaluationCriterionId { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Congress Congress { get; set; } = null!;
    public virtual EvaluationCriterion EvaluationCriterion { get; set; } = null!;
}
