using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ICongressPaymentPlanTranslationRepository : IAsyncRepository<CongressPaymentPlanTranslation, Guid>, IRepository<CongressPaymentPlanTranslation, Guid>
{
}
