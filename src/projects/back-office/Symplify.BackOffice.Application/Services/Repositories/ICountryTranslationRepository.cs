using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Reference.Translations;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ICountryTranslationRepository : IAsyncRepository<CountryTranslation, Guid>, IRepository<CountryTranslation, Guid>
{
}
