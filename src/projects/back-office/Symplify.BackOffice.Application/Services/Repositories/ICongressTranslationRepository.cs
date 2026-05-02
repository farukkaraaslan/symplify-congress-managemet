using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ICongressTranslationRepository : IAsyncRepository<CongressTranslation, Guid>, IRepository<CongressTranslation, Guid>
{
}
