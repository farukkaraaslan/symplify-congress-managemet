using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Communication;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface IMailOutboxMessageRepository : IAsyncRepository<MailOutboxMessage, Guid>, IRepository<MailOutboxMessage, Guid>
{
    Task AddRangeAsync(
        IReadOnlyCollection<MailOutboxMessage> messages,
        CancellationToken cancellationToken = default);

    Task<bool> MarkOpenedAsync(
        Guid trackingToken,
        DateTime openedAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically records successful SMTP transport without overwriting a delivery event that may
    /// already have arrived from SES in a different request/scope.
    /// </summary>
    Task<bool> MarkTransportSentAsync(
        Guid id,
        DateTime sentAt,
        int attemptCount,
        DateTime lastAttemptAt,
        string provider,
        MailDeliveryStatus initialDeliveryStatus,
        string? redactedHtmlBody,
        string updatedBy,
        CancellationToken cancellationToken = default);
}
