using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class CongressDocumentRepository : EfRepositoryBase<CongressDocument, BackOfficeDbContext, Guid>, ICongressDocumentRepository
{
    public CongressDocumentRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
