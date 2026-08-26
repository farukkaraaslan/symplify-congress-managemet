using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface IWorkflowTransitionEffectRepository : IAsyncRepository<WorkflowTransitionEffect, Guid>, IRepository<WorkflowTransitionEffect, Guid>
{
}
