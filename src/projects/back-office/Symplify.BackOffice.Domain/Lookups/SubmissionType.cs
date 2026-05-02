using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Lookups;

public class SubmissionType : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public string? Code { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<SubmissionTypeTranslation> Translations { get; set; } = new HashSet<SubmissionTypeTranslation>();
    public virtual ICollection<Symplify.BackOffice.Domain.Congress.CongressSubmissionType> CongressSubmissionTypes { get; set; } = new HashSet<Symplify.BackOffice.Domain.Congress.CongressSubmissionType>();
}
