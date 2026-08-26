using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Localization;

namespace Symplify.BackOffice.Domain.Workflow;

public class TransactionStatusTranslation : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public int TransactionStatusId { get; set; }
    public Guid LanguageId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public virtual TransactionStatus TransactionStatus { get; set; } = null!;
    public virtual Language Language { get; set; } = null!;
    public virtual ICollection<WorkflowTemplateTransition> WorkflowTemplateTransitions { get; set; } =
    new HashSet<WorkflowTemplateTransition>();
}
