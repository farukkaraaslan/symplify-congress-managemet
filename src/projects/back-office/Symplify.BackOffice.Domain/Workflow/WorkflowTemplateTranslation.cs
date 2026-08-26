using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Localization;

namespace Symplify.BackOffice.Domain.Workflow;

public class WorkflowTemplateTranslation : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid WorkflowTemplateId { get; set; }

    public Guid LanguageId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual WorkflowTemplate WorkflowTemplate { get; set; } = null!;

    public virtual Language Language { get; set; } = null!;
}
