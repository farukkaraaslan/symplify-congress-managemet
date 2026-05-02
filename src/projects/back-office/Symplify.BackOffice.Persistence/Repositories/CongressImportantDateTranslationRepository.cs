using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class CongressImportantDateTranslationRepository : EfRepositoryBase<CongressImportantDateTranslation, BackOfficeDbContext, Guid>, ICongressImportantDateTranslationRepository
{
    public CongressImportantDateTranslationRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
