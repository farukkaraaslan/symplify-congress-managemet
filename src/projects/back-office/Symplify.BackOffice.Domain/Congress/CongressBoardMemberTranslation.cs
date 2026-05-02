using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Localization;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressBoardMemberTranslation : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressBoardMemberId { get; set; }
    public Guid LanguageId { get; set; }
    public string FullName { get; set; } = null!;
    public string? Title { get; set; }
    public string? Institution { get; set; }
    public string? Biography { get; set; }

    public virtual CongressBoardMember CongressBoardMember { get; set; } = null!;
    public virtual Language Language { get; set; } = null!;
}
