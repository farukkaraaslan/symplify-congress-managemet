using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Submission;

public sealed class SubmissionExhibitionDetail : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid SubmissionId { get; set; }

    public string WorkName { get; set; } = string.Empty;

    public string? Dimensions { get; set; }

    public string Technique { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Address { get; set; } = string.Empty;

    public Submission Submission { get; set; } = null!;
}
