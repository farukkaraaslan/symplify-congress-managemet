using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class CongressPaymentPlanRepository : EfRepositoryBase<CongressPaymentPlan, BackOfficeDbContext, Guid>, ICongressPaymentPlanRepository
{
    public CongressPaymentPlanRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
