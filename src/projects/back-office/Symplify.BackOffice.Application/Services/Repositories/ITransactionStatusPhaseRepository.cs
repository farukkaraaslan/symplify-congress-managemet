using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface ITransactionStatusPhaseRepository
    : IAsyncRepository<TransactionStatusPhase, int>, IRepository<TransactionStatusPhase, int>
{
}
