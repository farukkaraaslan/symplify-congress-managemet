using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.Congresses.Queries.GetCloneSources;

public sealed class GetCongressCloneSourceListItemDto
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public int? EditionNumber { get; init; }

    public DateTime? StartDate { get; init; }

    public DateTime? EndDate { get; init; }

    public CongressStatus Status { get; init; }
}
