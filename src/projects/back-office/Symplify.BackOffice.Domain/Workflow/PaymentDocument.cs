using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Workflow;

public class PaymentDocument : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid? SubmissionId { get; set; }
    public Guid? CongressId { get; set; }
    public string FilePath { get; set; } = null!;
    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public long? Size { get; set; }
    public bool IsApproved { get; set; }

    public virtual Symplify.BackOffice.Domain.Submission.Submission? Submission { get; set; }
    public virtual Symplify.BackOffice.Domain.Congress.Congress? Congress { get; set; }
}
