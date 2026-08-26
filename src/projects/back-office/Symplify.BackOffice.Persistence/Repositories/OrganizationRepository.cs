using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class OrganizationRepository : EfRepositoryBase<Organization, BackOfficeDbContext, Guid>, IOrganizationRepository
{
    public OrganizationRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
