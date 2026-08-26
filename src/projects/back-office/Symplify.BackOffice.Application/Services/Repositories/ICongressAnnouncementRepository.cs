using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface ICongressAnnouncementRepository
    : IAsyncRepository<CongressAnnouncement, Guid>, IRepository<CongressAnnouncement, Guid>
{
}
