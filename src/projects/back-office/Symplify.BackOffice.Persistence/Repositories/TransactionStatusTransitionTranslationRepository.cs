using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class TransactionStatusTransitionTranslationRepository : EfRepositoryBase<TransactionStatusTransitionTranslation, BackOfficeDbContext, Guid>, ITransactionStatusTransitionTranslationRepository
{
    public TransactionStatusTransitionTranslationRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
