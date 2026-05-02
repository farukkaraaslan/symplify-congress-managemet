using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Tenant;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ITenantRepository : IAsyncRepository<Tenant, Guid>, IRepository<Tenant, Guid>
{
}
