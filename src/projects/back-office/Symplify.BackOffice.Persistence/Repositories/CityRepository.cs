using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Reference;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class CityRepository : EfRepositoryBase<City, BackOfficeDbContext, Guid>, ICityRepository
{
    public CityRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
