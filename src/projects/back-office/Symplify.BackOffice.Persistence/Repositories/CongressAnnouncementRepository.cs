using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class CongressAnnouncementRepository
    : EfRepositoryBase<CongressAnnouncement, BackOfficeDbContext, Guid>, ICongressAnnouncementRepository
{
    public CongressAnnouncementRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
