using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ICongressSectionTranslationRepository : IAsyncRepository<CongressSectionTranslation, Guid>, IRepository<CongressSectionTranslation, Guid>
{
}
