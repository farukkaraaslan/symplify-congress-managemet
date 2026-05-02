using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class CongressTopicRepository : EfRepositoryBase<CongressTopic, BackOfficeDbContext, Guid>, ICongressTopicRepository
{
    public CongressTopicRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
