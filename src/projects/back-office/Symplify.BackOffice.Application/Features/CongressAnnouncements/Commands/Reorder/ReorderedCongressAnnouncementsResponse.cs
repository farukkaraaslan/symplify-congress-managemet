namespace Symplify.BackOffice.Application.Features.CongressAnnouncements.Commands.Reorder;

public class ReorderedCongressAnnouncementsResponse
{
    public Guid CongressId { get; set; }

    public int UpdatedCount { get; set; }

    public IReadOnlyCollection<Guid> OrderedIds { get; set; } = Array.Empty<Guid>();
}
