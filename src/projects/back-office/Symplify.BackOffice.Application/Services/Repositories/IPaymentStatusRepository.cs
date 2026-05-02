using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Workflow;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface IPaymentStatusRepository : IAsyncRepository<PaymentStatus, int>, IRepository<PaymentStatus, int>
{
}
