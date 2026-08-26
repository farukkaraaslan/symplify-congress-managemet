using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Reference.Translations;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ICityTranslationRepository : IAsyncRepository<CityTranslation, Guid>, IRepository<CityTranslation, Guid>
{
}
