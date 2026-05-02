using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Tenant;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class TenantRepository : EfRepositoryBase<Tenant, BackOfficeDbContext, Guid>, ITenantRepository
{
    public TenantRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
