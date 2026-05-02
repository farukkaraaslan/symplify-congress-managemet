using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Submission;

public class Author : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Email { get; set; }
    public string? Institution { get; set; }
    public string? Orcid { get; set; }
    public bool IsCorrespondingAuthor { get; set; }

    public virtual ICollection<Submission> Submissions { get; set; } = new HashSet<Submission>();
}
