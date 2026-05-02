using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Tenant;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class TenantUserRepository : EfRepositoryBase<TenantUser, BackOfficeDbContext, Guid>, ITenantUserRepository
{
    public TenantUserRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
