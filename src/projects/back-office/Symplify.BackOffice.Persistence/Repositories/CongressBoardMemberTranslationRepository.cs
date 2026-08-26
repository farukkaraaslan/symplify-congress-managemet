using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class CongressBoardMemberTranslationRepository : EfRepositoryBase<CongressBoardMemberTranslation, BackOfficeDbContext, Guid>, ICongressBoardMemberTranslationRepository
{
    public CongressBoardMemberTranslationRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
