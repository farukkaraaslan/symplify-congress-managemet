using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Organization;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface IOrganizationRepository : IAsyncRepository<Organization, Guid>, IRepository<Organization, Guid>
{
}
