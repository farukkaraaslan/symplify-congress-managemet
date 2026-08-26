namespace Symplify.BackOffice.Application.Features.BulkEmails.Queries.GetHistory;

public sealed class GetBulkEmailHistoryResponse
{
    public int TotalCount { get; set; }

    public int PageIndex { get; set; }

    public int PageSize { get; set; }

    public int TotalPages { get; set; }

    public int PendingCount { get; set; }

    public int SentCount { get; set; }

    public int FailedCount { get; set; }

    public int CancelledCount { get; set; }

    public int OpenedCount { get; set; }

    public IReadOnlyList<BulkEmailHistoryItemDto> Items { get; set; } = Array.Empty<BulkEmailHistoryItemDto>();
}
