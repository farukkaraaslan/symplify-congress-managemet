using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressSlider : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressId { get; set; }
    public string ImagePath { get; set; } = null!;
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Congress Congress { get; set; } = null!;
    public virtual ICollection<CongressSliderTranslation> Translations { get; set; } = new HashSet<CongressSliderTranslation>();
}
