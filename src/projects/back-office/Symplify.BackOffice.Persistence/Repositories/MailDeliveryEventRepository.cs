using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Communication;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public sealed class MailDeliveryEventRepository
    : EfRepositoryBase<MailDeliveryEvent, BackOfficeDbContext, Guid>, IMailDeliveryEventRepository
{
    public MailDeliveryEventRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
