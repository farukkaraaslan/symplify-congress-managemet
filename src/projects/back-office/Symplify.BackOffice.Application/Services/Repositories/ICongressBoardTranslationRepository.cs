using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ICongressBoardTranslationRepository : IAsyncRepository<CongressBoardTranslation, Guid>, IRepository<CongressBoardTranslation, Guid>
{
}
