using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class CongressTransactionStatusTransitionRepository
    : EfRepositoryBase<CongressTransactionStatusTransition, BackOfficeDbContext, Guid>, ICongressTransactionStatusTransitionRepository
{
    public CongressTransactionStatusTransitionRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
