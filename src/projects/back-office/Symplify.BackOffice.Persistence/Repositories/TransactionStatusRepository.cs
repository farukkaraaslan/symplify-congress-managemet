using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class TransactionStatusRepository : EfRepositoryBase<TransactionStatus, BackOfficeDbContext, int>, ITransactionStatusRepository
{
    public TransactionStatusRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
