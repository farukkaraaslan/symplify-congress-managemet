using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Localization;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressSliderTranslation : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressSliderId { get; set; }
    public Guid LanguageId { get; set; }
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? ButtonText { get; set; }
    public string? ButtonUrl { get; set; }

    public virtual CongressSlider CongressSlider { get; set; } = null!;
    public virtual Language Language { get; set; } = null!;
}
