using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Workflow;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ITransactionStatusTranslationRepository : IAsyncRepository<TransactionStatusTranslation, Guid>, IRepository<TransactionStatusTranslation, Guid>
{
}
