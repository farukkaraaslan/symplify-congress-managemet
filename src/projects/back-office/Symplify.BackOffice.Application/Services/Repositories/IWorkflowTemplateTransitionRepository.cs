using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface IWorkflowTemplateTransitionRepository : IAsyncRepository<WorkflowTemplateTransition, Guid>, IRepository<WorkflowTemplateTransition, Guid>
{
}
