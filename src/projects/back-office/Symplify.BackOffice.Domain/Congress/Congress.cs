using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Common;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Tenant;

namespace Symplify.BackOffice.Domain.Congress;

public class Congress : Entity<Guid>, IEntityTimestamps, IAuditable, IAggregateRoot
{
    public Guid TenantId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Slug { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public CongressStatus Status { get; set; } = CongressStatus.Draft;
    public bool IsActive { get; set; } = true;

    public virtual Symplify.BackOffice.Domain.Tenant.Tenant Tenant { get; set; } = null!;
    public virtual ICollection<CongressTranslation> Translations { get; set; } = new HashSet<CongressTranslation>();
    public virtual ICollection<CongressSection> Sections { get; set; } = new HashSet<CongressSection>();
    public virtual ICollection<CongressSlider> Sliders { get; set; } = new HashSet<CongressSlider>();
    public virtual ICollection<CongressBoard> Boards { get; set; } = new HashSet<CongressBoard>();
    public virtual ICollection<CongressImportantDate> ImportantDates { get; set; } = new HashSet<CongressImportantDate>();
    public virtual ICollection<CongressPaymentPlan> PaymentPlans { get; set; } = new HashSet<CongressPaymentPlan>();
    public virtual ICollection<CongressDocument> Documents { get; set; } = new HashSet<CongressDocument>();
    public virtual ICollection<CongressTopic> Topics { get; set; } = new HashSet<CongressTopic>();
    public virtual ICollection<CongressSubmissionType> SubmissionTypes { get; set; } = new HashSet<CongressSubmissionType>();
    public virtual ICollection<CongressEvaluationCriterion> EvaluationCriteria { get; set; } = new HashSet<CongressEvaluationCriterion>();
}
