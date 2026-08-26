using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Domain.Congress;

public sealed class CongressReviewer : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressId { get; set; }

    public Guid ReviewerId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? InvitedAt { get; set; }

    public DateTime? AcceptedAt { get; set; }

    public DateTime? RejectedAt { get; set; }

    public string? ExpertiseKeywords { get; set; }

    public string? Note { get; set; }

    public Congress Congress { get; set; } = null!;

    public Reviewer Reviewer { get; set; } = null!;
}
