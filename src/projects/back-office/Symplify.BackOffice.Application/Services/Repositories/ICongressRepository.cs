using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ICongressRepository : IAsyncRepository<Congress, Guid>, IRepository<Congress, Guid>
{
}
