using Core.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Communication;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public sealed class MailOutboxMessageRepository : EfRepositoryBase<MailOutboxMessage, BackOfficeDbContext, Guid>, IMailOutboxMessageRepository
{
    private readonly BackOfficeDbContext _context;

    public MailOutboxMessageRepository(BackOfficeDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(
        IReadOnlyCollection<MailOutboxMessage> messages,
        CancellationToken cancellationToken = default)
    {
        if (messages.Count == 0)
            return;

        await _context.MailOutboxMessages.AddRangeAsync(messages, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> MarkOpenedAsync(
        Guid trackingToken,
        DateTime openedAt,
        CancellationToken cancellationToken = default)
    {
        if (trackingToken == Guid.Empty)
            return false;

        int affectedRows = await _context.MailOutboxMessages
            .Where(message =>
                message.TrackingToken == trackingToken &&
                message.DeletedDate == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        message => message.FirstOpenedAt,
                        message => message.FirstOpenedAt ?? openedAt)
                    .SetProperty(message => message.LastOpenedAt, openedAt)
                    .SetProperty(message => message.OpenCount, message => message.OpenCount + 1)
                    .SetProperty(message => message.UpdatedDate, openedAt)
                    .SetProperty(message => message.UpdatedBy, "MailOpenTracker"),
                cancellationToken);

        return affectedRows > 0;
    }

    public async Task<bool> MarkTransportSentAsync(
        Guid id,
        DateTime sentAt,
        int attemptCount,
        DateTime lastAttemptAt,
        string provider,
        MailDeliveryStatus initialDeliveryStatus,
        string? redactedHtmlBody,
        string updatedBy,
        CancellationToken cancellationToken = default)
    {
        int affectedRows = await _context.MailOutboxMessages
            .Where(message => message.Id == id && message.DeletedDate == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(message => message.Status, MailOutboxStatus.Sent)
                    .SetProperty(message => message.SentAt, sentAt)
                    .SetProperty(message => message.AttemptCount, attemptCount)
                    .SetProperty(message => message.LastAttemptAt, lastAttemptAt)
                    .SetProperty(message => message.LastError, (string?)null)
                    .SetProperty(message => message.Provider, message => message.Provider ?? provider)
                    .SetProperty(
                        message => message.DeliveryStatus,
                        message => message.LastDeliveryEventAt == null
                            ? initialDeliveryStatus
                            : message.DeliveryStatus)
                    .SetProperty(
                        message => message.HtmlBody,
                        message => redactedHtmlBody ?? message.HtmlBody)
                    .SetProperty(message => message.UpdatedDate, sentAt)
                    .SetProperty(message => message.UpdatedBy, updatedBy),
                cancellationToken);

        return affectedRows > 0;
    }

}
