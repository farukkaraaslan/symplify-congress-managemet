using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Tenant;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class TenantApiKeyRepository : EfRepositoryBase<TenantApiKey, BackOfficeDbContext, Guid>, ITenantApiKeyRepository
{
    public TenantApiKeyRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
