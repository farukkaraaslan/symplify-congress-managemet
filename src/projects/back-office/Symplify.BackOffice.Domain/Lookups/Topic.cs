using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Lookups;

public class Topic : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public string? Code { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<TopicTranslation> Translations { get; set; } = new HashSet<TopicTranslation>();
    public virtual ICollection<Congress.CongressTopic> CongressTopics { get; set; } = new HashSet<Congress.CongressTopic>();
}
