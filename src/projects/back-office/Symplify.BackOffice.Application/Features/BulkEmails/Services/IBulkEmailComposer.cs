using Symplify.BackOffice.Application.Features.BulkEmails.Dtos;

namespace Symplify.BackOffice.Application.Features.BulkEmails.Services;

public interface IBulkEmailComposer
{
    Task<PreparedBulkEmailTemplate> PrepareAsync(
        Guid congressId,
        string? culture,
        string subject,
        string title,
        string bodyText,
        CancellationToken cancellationToken = default);

    string RenderSubject(PreparedBulkEmailTemplate template, string recipientName);

    string RenderHtmlBody(PreparedBulkEmailTemplate template, string recipientName);
}
