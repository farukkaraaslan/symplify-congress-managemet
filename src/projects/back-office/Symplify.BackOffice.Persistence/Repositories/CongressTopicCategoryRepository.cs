using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class CongressTopicCategoryRepository
    : EfRepositoryBase<CongressTopicCategory, BackOfficeDbContext, Guid>, ICongressTopicCategoryRepository
{
    public CongressTopicCategoryRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
