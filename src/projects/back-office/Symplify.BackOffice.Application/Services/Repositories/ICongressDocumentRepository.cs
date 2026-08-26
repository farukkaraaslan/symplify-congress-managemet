using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ICongressDocumentRepository : IAsyncRepository<CongressDocument, Guid>, IRepository<CongressDocument, Guid>
{
}
