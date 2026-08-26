using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.BulkEmails.Queries.GetHistory;

public sealed class BulkEmailHistoryItemDto
{
    public Guid Id { get; set; }

    public Guid BatchId { get; set; }

    public string RecipientName { get; set; } = string.Empty;

    public string RecipientEmail { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public BulkEmailAudienceType AudienceType { get; set; }

    public MailOutboxStatus Status { get; set; }

    public int AttemptCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? FirstOpenedAt { get; set; }

    public DateTime? LastOpenedAt { get; set; }

    public int OpenCount { get; set; }

    public string? LastError { get; set; }
}
