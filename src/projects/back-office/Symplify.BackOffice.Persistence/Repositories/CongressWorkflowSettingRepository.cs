using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class CongressWorkflowSettingRepository
    : EfRepositoryBase<CongressWorkflowSetting, BackOfficeDbContext, Guid>, ICongressWorkflowSettingRepository
{
    public CongressWorkflowSettingRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
