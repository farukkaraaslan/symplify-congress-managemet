using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class WorkflowTemplateRepository : EfRepositoryBase<WorkflowTemplate, BackOfficeDbContext, Guid>, IWorkflowTemplateRepository
{
    public WorkflowTemplateRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
