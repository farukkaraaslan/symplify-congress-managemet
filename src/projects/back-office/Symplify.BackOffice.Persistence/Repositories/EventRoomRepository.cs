using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class EventRoomRepository : EfRepositoryBase<EventRoom, BackOfficeDbContext, Guid>, IEventRoomRepository
{
    public EventRoomRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
