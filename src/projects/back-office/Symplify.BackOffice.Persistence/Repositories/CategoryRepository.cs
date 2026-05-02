using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class TopicRepository : EfRepositoryBase<Topic, BackOfficeDbContext, Guid>, ITopicRepository
{
    public TopicRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
