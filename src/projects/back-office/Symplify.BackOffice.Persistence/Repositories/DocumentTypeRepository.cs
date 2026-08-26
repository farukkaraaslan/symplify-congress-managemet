using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class DocumentTypeRepository : EfRepositoryBase<DocumentType, BackOfficeDbContext, Guid>, IDocumentTypeRepository
{
    public DocumentTypeRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
