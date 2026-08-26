using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class CongressAnnouncementTranslationRepository
    : EfRepositoryBase<CongressAnnouncementTranslation, BackOfficeDbContext, Guid>, ICongressAnnouncementTranslationRepository
{
    public CongressAnnouncementTranslationRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
