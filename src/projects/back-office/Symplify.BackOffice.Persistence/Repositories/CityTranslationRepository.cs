using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Reference.Translations;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class CityTranslationRepository : EfRepositoryBase<CityTranslation, BackOfficeDbContext, Guid>, ICityTranslationRepository
{
    public CityTranslationRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
