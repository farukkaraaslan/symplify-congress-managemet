using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class TransactionStatusTranslationRepository : EfRepositoryBase<TransactionStatusTranslation, BackOfficeDbContext, Guid>, ITransactionStatusTranslationRepository
{
    public TransactionStatusTranslationRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
