using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class WorkflowTemplateTranslationRepository
    : EfRepositoryBase<WorkflowTemplateTranslation, BackOfficeDbContext, Guid>, IWorkflowTemplateTranslationRepository
{
    public WorkflowTemplateTranslationRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
