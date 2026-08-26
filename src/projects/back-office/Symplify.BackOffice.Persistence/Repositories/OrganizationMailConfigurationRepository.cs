using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public sealed class OrganizationMailConfigurationRepository :
    EfRepositoryBase<OrganizationMailConfiguration, BackOfficeDbContext, Guid>,
    IOrganizationMailConfigurationRepository
{
    public OrganizationMailConfigurationRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
