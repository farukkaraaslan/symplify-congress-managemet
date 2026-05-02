using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Lookups;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface IDocumentTypeTranslationRepository : IAsyncRepository<DocumentTypeTranslation, Guid>, IRepository<DocumentTypeTranslation, Guid>
{
}
