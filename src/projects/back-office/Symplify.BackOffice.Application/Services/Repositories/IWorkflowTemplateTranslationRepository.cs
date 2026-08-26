using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface IWorkflowTemplateTranslationRepository : IAsyncRepository<WorkflowTemplateTranslation, Guid>, IRepository<WorkflowTemplateTranslation, Guid>
{
}
