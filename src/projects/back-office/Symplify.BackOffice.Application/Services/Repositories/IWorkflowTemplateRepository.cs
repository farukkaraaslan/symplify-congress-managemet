using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface IWorkflowTemplateRepository : IAsyncRepository<WorkflowTemplate, Guid>, IRepository<WorkflowTemplate, Guid>
{
}
