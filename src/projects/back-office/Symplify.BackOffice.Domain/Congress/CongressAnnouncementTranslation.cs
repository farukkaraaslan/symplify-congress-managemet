using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Localization;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressAnnouncementTranslation : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressAnnouncementId { get; set; }

    public Guid LanguageId { get; set; }

    public string Title { get; set; } = null!;

    public string? Summary { get; set; }

    public string? Content { get; set; }

    public string? SeoTitle { get; set; }

    public string? SeoDescription { get; set; }

    public virtual CongressAnnouncement CongressAnnouncement { get; set; } = null!;

    public virtual Language Language { get; set; } = null!;
}
