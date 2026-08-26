using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class CongressPaymentPlanTranslationRepository : EfRepositoryBase<CongressPaymentPlanTranslation, BackOfficeDbContext, Guid>, ICongressPaymentPlanTranslationRepository
{
    public CongressPaymentPlanTranslationRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
