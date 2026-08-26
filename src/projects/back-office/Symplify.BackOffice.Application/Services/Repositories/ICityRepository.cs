using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Reference;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ICityRepository : IAsyncRepository<City, Guid>, IRepository<City, Guid>
{
}
