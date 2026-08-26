using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class TitleTranslationRepository : EfRepositoryBase<TitleTranslation, BackOfficeDbContext, Guid>, ITitleTranslationRepository
{
    public TitleTranslationRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
