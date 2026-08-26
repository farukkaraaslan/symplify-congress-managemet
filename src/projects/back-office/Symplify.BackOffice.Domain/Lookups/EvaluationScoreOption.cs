using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Lookups;

public class EvaluationScoreOption : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public decimal Value { get; set; }
    public string? Label { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
}
