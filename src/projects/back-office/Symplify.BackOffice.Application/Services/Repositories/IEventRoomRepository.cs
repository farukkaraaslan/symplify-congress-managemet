using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Lookups;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface IEventRoomRepository : IAsyncRepository<EventRoom, Guid>, IRepository<EventRoom, Guid>
{
}
