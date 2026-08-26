namespace Symplify.BackOffice.Application.Features.BulkEmails.Dtos;

public sealed class PreparedBulkEmailTemplate
{
    public string SubjectTemplate { get; init; } = string.Empty;

    public string HtmlBodyTemplate { get; init; } = string.Empty;

    public string RecipientPlaceholder { get; init; } = string.Empty;

    public string CongressTitle { get; init; } = string.Empty;

    public IReadOnlyList<string> WarningLinks { get; init; } = Array.Empty<string>();
}
