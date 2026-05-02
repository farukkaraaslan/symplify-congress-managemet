using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ICongressBoardRepository : IAsyncRepository<CongressBoard, Guid>, IRepository<CongressBoard, Guid>
{
}
