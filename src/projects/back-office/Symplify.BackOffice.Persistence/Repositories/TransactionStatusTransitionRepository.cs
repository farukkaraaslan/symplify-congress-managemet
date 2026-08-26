using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class TransactionStatusTransitionRepository : EfRepositoryBase<TransactionStatusTransition, BackOfficeDbContext, int>, ITransactionStatusTransitionRepository
{
    public TransactionStatusTransitionRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
