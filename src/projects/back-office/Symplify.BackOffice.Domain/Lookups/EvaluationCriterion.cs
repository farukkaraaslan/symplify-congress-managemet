using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Lookups;

public class EvaluationCriterion : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public string? Code { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<EvaluationCriterionTranslation> Translations { get; set; } = new HashSet<EvaluationCriterionTranslation>();
    public virtual ICollection<Symplify.BackOffice.Domain.Congress.CongressEvaluationCriterion> CongressEvaluationCriteria { get; set; } = new HashSet<Symplify.BackOffice.Domain.Congress.CongressEvaluationCriterion>();
}
