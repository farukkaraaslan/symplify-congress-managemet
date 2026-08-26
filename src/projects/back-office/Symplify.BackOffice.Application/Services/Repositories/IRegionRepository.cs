using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Reference;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface IRegionRepository : IAsyncRepository<Region, Guid>, IRepository<Region, Guid>
{
}
