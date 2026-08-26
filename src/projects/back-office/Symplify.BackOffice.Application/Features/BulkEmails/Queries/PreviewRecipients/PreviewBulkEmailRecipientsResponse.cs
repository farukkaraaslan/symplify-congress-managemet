using Symplify.BackOffice.Application.Features.BulkEmails.Dtos;

namespace Symplify.BackOffice.Application.Features.BulkEmails.Queries.PreviewRecipients;

public sealed class PreviewBulkEmailRecipientsResponse
{
    public int RecipientCount { get; set; }

    public int FilteredCount { get; set; }

    public int InvalidEmailCount { get; set; }

    public int PageIndex { get; set; }

    public int PageSize { get; set; }

    public int TotalPages { get; set; }

    public IReadOnlyList<BulkEmailRecipientDto> Recipients { get; set; } = Array.Empty<BulkEmailRecipientDto>();
}
