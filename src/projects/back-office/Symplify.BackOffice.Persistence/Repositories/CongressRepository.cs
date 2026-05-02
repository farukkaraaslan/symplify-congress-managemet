using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class CongressRepository : EfRepositoryBase<Congress, BackOfficeDbContext, Guid>, ICongressRepository
{
    public CongressRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
