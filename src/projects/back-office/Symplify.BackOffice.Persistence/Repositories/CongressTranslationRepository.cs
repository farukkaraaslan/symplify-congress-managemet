using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class CongressTranslationRepository : EfRepositoryBase<CongressTranslation, BackOfficeDbContext, Guid>, ICongressTranslationRepository
{
    public CongressTranslationRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
