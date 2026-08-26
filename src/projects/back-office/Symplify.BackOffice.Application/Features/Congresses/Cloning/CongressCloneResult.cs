namespace Symplify.BackOffice.Application.Features.Congresses.Cloning;

public sealed class CongressCloneResult
{
    public Guid SourceCongressId { get; init; }

    public Guid TargetCongressId { get; init; }

    public IReadOnlyDictionary<CongressCloneModule, int> CopiedRecordCounts { get; init; }
        = new Dictionary<CongressCloneModule, int>();
}
