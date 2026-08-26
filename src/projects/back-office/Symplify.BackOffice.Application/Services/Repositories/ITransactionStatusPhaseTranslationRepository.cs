using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface ITransactionStatusPhaseTranslationRepository
    : IAsyncRepository<TransactionStatusPhaseTranslation, Guid>, IRepository<TransactionStatusPhaseTranslation, Guid>
{
}
