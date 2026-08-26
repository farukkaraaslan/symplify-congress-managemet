using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class DocumentTypeTranslationRepository : EfRepositoryBase<DocumentTypeTranslation, BackOfficeDbContext, Guid>, IDocumentTypeTranslationRepository
{
    public DocumentTypeTranslationRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
