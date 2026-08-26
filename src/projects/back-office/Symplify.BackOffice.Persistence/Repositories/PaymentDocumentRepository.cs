using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class PaymentDocumentRepository : EfRepositoryBase<PaymentDocument, BackOfficeDbContext, Guid>, IPaymentDocumentRepository
{
    public PaymentDocumentRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
