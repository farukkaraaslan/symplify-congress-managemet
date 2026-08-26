using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface ICongressTransactionStatusTransitionRepository : IAsyncRepository<CongressTransactionStatusTransition, Guid>, IRepository<CongressTransactionStatusTransition, Guid>
{
}
