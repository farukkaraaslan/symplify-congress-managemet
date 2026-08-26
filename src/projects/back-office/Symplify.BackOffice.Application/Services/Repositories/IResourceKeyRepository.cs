using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Localization;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface IResourceKeyRepository : IAsyncRepository<ResourceKey, Guid>, IRepository<ResourceKey, Guid>
{
}
