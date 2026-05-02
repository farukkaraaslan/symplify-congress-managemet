using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Workflow;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ITransactionStatusRepository : IAsyncRepository<TransactionStatus, int>, IRepository<TransactionStatus, int>
{
}
