using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface ICongressAnnouncementTranslationRepository
    : IAsyncRepository<CongressAnnouncementTranslation, Guid>, IRepository<CongressAnnouncementTranslation, Guid>
{
}
