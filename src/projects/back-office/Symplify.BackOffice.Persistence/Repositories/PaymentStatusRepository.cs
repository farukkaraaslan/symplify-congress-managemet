using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class PaymentStatusRepository : EfRepositoryBase<PaymentStatus, BackOfficeDbContext, int>, IPaymentStatusRepository
{
    public PaymentStatusRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
