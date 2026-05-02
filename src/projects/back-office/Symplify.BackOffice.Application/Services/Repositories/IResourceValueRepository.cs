using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Localization;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface IResourceValueRepository : IAsyncRepository<ResourceValue, Guid>, IRepository<ResourceValue, Guid>
{
}
