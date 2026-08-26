namespace Symplify.BackOffice.Application.Features.BulkEmails.Queries.PreviewContent;

public sealed class PreviewBulkEmailContentResponse
{
    public bool CanSend { get; set; }

    public string Subject { get; set; } = string.Empty;

    public string HtmlBody { get; set; } = string.Empty;

    public IReadOnlyList<string> UnsafeLinks { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> WarningLinks { get; set; } = Array.Empty<string>();
}
