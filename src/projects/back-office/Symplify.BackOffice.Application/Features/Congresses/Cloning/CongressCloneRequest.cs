namespace Symplify.BackOffice.Application.Features.Congresses.Cloning;

public sealed class CongressCloneRequest
{
    public Guid SourceCongressId { get; init; }

    public Guid TargetCongressId { get; init; }

    public bool ShiftRelativeDates { get; init; } = true;

    public IReadOnlyCollection<CongressCloneModule> Modules { get; init; }
        = Array.Empty<CongressCloneModule>();
}
