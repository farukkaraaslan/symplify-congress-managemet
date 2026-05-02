using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Reference;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class RegionRepository : EfRepositoryBase<Region, BackOfficeDbContext, Guid>, IRegionRepository
{
    public RegionRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
