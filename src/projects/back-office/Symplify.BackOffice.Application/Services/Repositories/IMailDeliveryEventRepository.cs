using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Communication;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface IMailDeliveryEventRepository : IAsyncRepository<MailDeliveryEvent, Guid>, IRepository<MailDeliveryEvent, Guid>
{
}
