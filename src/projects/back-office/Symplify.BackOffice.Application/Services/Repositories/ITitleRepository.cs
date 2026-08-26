using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Lookups;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ITitleRepository : IAsyncRepository<Title, Guid>, IRepository<Title, Guid>
{
}
