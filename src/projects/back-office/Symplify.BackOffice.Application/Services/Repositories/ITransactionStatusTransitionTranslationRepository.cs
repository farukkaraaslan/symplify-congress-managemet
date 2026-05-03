using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface ITransactionStatusTransitionTranslationRepository
    : IAsyncRepository<TransactionStatusTransitionTranslation, Guid>, IRepository<TransactionStatusTransitionTranslation, Guid>
{
}
