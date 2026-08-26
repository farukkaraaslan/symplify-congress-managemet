using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class CongressTopicCategoryTranslationRepository
    : EfRepositoryBase<CongressTopicCategoryTranslation, BackOfficeDbContext, Guid>, ICongressTopicCategoryTranslationRepository
{
    public CongressTopicCategoryTranslationRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
