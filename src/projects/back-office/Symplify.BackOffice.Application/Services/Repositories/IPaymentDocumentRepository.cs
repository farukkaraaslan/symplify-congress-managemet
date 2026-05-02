using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Workflow;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface IPaymentDocumentRepository : IAsyncRepository<PaymentDocument, Guid>, IRepository<PaymentDocument, Guid>
{
}
