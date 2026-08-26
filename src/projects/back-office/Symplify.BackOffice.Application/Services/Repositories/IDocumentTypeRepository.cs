using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Lookups;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface IDocumentTypeRepository : IAsyncRepository<DocumentType, Guid>, IRepository<DocumentType, Guid>
{
}
