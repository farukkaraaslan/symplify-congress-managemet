using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Reference;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class StateRepository : EfRepositoryBase<State, BackOfficeDbContext, Guid>, IStateRepository
{
    public StateRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
