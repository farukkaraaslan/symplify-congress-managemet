using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Reference.Translations;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface IRegionTranslationRepository : IAsyncRepository<RegionTranslation, Guid>, IRepository<RegionTranslation, Guid>
{
}
