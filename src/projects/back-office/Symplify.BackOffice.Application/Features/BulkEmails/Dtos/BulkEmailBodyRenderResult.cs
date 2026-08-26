namespace Symplify.BackOffice.Application.Features.BulkEmails.Dtos;

public sealed class BulkEmailBodyRenderResult
{
    public string Html { get; init; } = string.Empty;

    public IReadOnlyList<string> UnsafeLinks { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> WarningLinks { get; init; } = Array.Empty<string>();
}
