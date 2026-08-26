namespace Symplify.BackOffice.Application.Features.CongressSections.Commands.Reorder;

public sealed class ReorderedCongressSectionResponse
{
    public Guid CongressId { get; set; }
    public int UpdatedCount { get; set; }
    public IReadOnlyCollection<Guid> OrderedIds { get; set; } = Array.Empty<Guid>();
}
