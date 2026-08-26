using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Reference.Translations;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class CountryTranslationRepository : EfRepositoryBase<CountryTranslation, BackOfficeDbContext, Guid>, ICountryTranslationRepository
{
    public CountryTranslationRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
