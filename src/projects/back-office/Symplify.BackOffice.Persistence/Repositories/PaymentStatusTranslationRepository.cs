using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class PaymentStatusTranslationRepository : EfRepositoryBase<PaymentStatusTranslation, BackOfficeDbContext, Guid>, IPaymentStatusTranslationRepository
{
    public PaymentStatusTranslationRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
