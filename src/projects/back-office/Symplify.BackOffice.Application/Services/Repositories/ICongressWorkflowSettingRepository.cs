using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface ICongressWorkflowSettingRepository : IAsyncRepository<CongressWorkflowSetting, Guid>, IRepository<CongressWorkflowSetting, Guid>
{
}
