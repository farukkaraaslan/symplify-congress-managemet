using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class TopicTranslationRepository : EfRepositoryBase<TopicTranslation, BackOfficeDbContext, Guid>, ITopicTranslationRepository
{
    public TopicTranslationRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
