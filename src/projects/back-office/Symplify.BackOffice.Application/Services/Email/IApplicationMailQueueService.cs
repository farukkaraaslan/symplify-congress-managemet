using Symplify.BackOffice.Domain.Communication;

namespace Symplify.BackOffice.Application.Services.Email;

public interface IApplicationMailQueueService
{
    Task<MailOutboxMessage> QueueAsync(
        MailQueueRequest request,
        CancellationToken cancellationToken = default);
}
