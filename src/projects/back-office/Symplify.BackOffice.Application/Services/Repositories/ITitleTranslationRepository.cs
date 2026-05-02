using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Lookups;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ITitleTranslationRepository : IAsyncRepository<TitleTranslation, Guid>, IRepository<TitleTranslation, Guid>
{
}
