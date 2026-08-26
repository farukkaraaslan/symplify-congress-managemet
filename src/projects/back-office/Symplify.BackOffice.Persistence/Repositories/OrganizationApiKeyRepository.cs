using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class OrganizationApiKeyRepository : EfRepositoryBase<OrganizationApiKey, BackOfficeDbContext, Guid>, IOrganizationApiKeyRepository
{
    public OrganizationApiKeyRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
