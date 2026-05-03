using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class WorkflowTemplateTransitionRepository
    : EfRepositoryBase<WorkflowTemplateTransition, BackOfficeDbContext, Guid>, IWorkflowTemplateTransitionRepository
{
    public WorkflowTemplateTransitionRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
