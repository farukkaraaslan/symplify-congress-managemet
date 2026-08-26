using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class CongressDocumentTranslationRepository
    : EfRepositoryBase<CongressDocumentTranslation, BackOfficeDbContext, Guid>, ICongressDocumentTranslationRepository
{
    public CongressDocumentTranslationRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
