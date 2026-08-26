using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public sealed class WorkflowTransitionConditionRepository : EfRepositoryBase<WorkflowTransitionCondition, BackOfficeDbContext, Guid>, IWorkflowTransitionConditionRepository
{
    public WorkflowTransitionConditionRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
