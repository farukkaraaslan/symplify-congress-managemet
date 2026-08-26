using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public sealed class WorkflowTransitionEffectRepository : EfRepositoryBase<WorkflowTransitionEffect, BackOfficeDbContext, Guid>, IWorkflowTransitionEffectRepository
{
    public WorkflowTransitionEffectRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
