using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ICongressPaymentPlanRepository : IAsyncRepository<CongressPaymentPlan, Guid>, IRepository<CongressPaymentPlan, Guid>
{
}
