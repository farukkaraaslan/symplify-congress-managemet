using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Organization;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface IOrganizationMailConfigurationRepository :
    IAsyncRepository<OrganizationMailConfiguration, Guid>,
    IRepository<OrganizationMailConfiguration, Guid>
{
}
