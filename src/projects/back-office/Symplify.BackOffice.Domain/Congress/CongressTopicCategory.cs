using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressTopicCategory : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressId { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Congress Congress { get; set; } = null!;
    public virtual ICollection<CongressTopicCategoryTranslation> Translations { get; set; }
        = new HashSet<CongressTopicCategoryTranslation>();
    public virtual ICollection<CongressTopic> Topics { get; set; }
        = new HashSet<CongressTopic>();
}
