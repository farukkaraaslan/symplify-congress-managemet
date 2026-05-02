using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Tenant;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ITenantUserRepository : IAsyncRepository<TenantUser, Guid>, IRepository<TenantUser, Guid>
{
}
