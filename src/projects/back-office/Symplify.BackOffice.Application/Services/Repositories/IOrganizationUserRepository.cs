using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Organization;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface IOrganizationUserRepository : IAsyncRepository<OrganizationUser, Guid>, IRepository<OrganizationUser, Guid>
{
}
