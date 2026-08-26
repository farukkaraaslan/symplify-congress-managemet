using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface IWorkflowTransitionConditionRepository : IAsyncRepository<WorkflowTransitionCondition, Guid>, IRepository<WorkflowTransitionCondition, Guid>
{
}
