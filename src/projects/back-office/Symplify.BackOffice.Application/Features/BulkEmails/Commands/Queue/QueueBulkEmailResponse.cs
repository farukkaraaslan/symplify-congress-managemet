namespace Symplify.BackOffice.Application.Features.BulkEmails.Commands.Queue;

public sealed class QueueBulkEmailResponse
{
    public Guid BatchId { get; set; }

    public int QueuedCount { get; set; }

    public int InvalidEmailCount { get; set; }

    public IReadOnlyList<string> WarningLinks { get; set; } = Array.Empty<string>();
}
