using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Common;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressProgramPlan : Entity<Guid>, IEntityTimestamps, IAuditable, IAggregateRoot
{
    public Guid CongressId { get; set; }
    public string Name { get; set; } = "Program Taslağı";
    public CongressProgramPlanStatus Status { get; set; } = CongressProgramPlanStatus.Draft;
    public int VersionNo { get; set; } = 1;
    public int DefaultPresentationDurationMinutes { get; set; } = 10;
    public int DefaultSessionDurationMinutes { get; set; } = 120;
    public int DefaultQuestionAnswerDurationMinutes { get; set; } = 10;
    public int DefaultBreakDurationMinutes { get; set; } = 30;
    public DateTime? LastGeneratedAt { get; set; }
    public Guid? LastGeneratedByUserId { get; set; }
    public string? SubmissionFilterJson { get; set; }
    public string? EligibleSubmissionIdsJson { get; set; }

    public virtual Congress Congress { get; set; } = null!;
    public virtual ICollection<CongressProgramDay> Days { get; set; } = new HashSet<CongressProgramDay>();
}
