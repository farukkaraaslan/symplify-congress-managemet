using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class CongressBoardTranslationRepository : EfRepositoryBase<CongressBoardTranslation, BackOfficeDbContext, Guid>, ICongressBoardTranslationRepository
{
    public CongressBoardTranslationRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
