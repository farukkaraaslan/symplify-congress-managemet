using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class CongressSectionTranslationRepository : EfRepositoryBase<CongressSectionTranslation, BackOfficeDbContext, Guid>, ICongressSectionTranslationRepository
{
    public CongressSectionTranslationRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
