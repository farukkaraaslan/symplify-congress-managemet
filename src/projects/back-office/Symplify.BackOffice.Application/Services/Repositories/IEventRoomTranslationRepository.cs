using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Lookups;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface IEventRoomTranslationRepository : IAsyncRepository<EventRoomTranslation, Guid>, IRepository<EventRoomTranslation, Guid>
{
}
