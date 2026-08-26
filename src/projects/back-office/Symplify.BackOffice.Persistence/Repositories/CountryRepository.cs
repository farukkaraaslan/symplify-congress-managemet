using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Reference;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class CountryRepository : EfRepositoryBase<Country, BackOfficeDbContext, Guid>, ICountryRepository
{
    public CountryRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
