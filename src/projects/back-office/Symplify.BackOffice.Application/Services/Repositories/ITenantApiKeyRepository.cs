using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Tenant;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ITenantApiKeyRepository : IAsyncRepository<TenantApiKey, Guid>, IRepository<TenantApiKey, Guid>
{
}
