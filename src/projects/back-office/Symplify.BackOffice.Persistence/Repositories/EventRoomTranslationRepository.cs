using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class EventRoomTranslationRepository : EfRepositoryBase<EventRoomTranslation, BackOfficeDbContext, Guid>, IEventRoomTranslationRepository
{
    public EventRoomTranslationRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
