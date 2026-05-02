using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Reference.Translations;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class RegionTranslationRepository : EfRepositoryBase<RegionTranslation, BackOfficeDbContext, Guid>, IRegionTranslationRepository
{
    public RegionTranslationRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
