using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Organization;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface IOrganizationApiKeyRepository : IAsyncRepository<OrganizationApiKey, Guid>, IRepository<OrganizationApiKey, Guid>
{
}
