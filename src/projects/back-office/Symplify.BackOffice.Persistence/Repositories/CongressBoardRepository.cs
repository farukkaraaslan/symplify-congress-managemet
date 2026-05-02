using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class CongressBoardRepository : EfRepositoryBase<CongressBoard, BackOfficeDbContext, Guid>, ICongressBoardRepository
{
    public CongressBoardRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
