using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Localization;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressTranslation : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressId { get; set; }
    public Guid LanguageId { get; set; }
    public string Title { get; set; } = null!;
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public string? LogoPath { get; set; }

    public virtual Congress Congress { get; set; } = null!;
    public virtual Language Language { get; set; } = null!;
}
