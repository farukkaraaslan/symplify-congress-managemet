using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Workflow;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface IPaymentStatusTranslationRepository : IAsyncRepository<PaymentStatusTranslation, Guid>, IRepository<PaymentStatusTranslation, Guid>
{
}
