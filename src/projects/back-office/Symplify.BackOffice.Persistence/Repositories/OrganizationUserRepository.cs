using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class OrganizationUserRepository : EfRepositoryBase<OrganizationUser, BackOfficeDbContext, Guid>, IOrganizationUserRepository
{
    public OrganizationUserRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
