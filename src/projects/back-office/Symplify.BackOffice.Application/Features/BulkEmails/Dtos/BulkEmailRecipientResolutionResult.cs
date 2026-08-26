namespace Symplify.BackOffice.Application.Features.BulkEmails.Dtos;

public sealed class BulkEmailRecipientResolutionResult
{
    public IReadOnlyList<BulkEmailRecipientDto> Recipients { get; init; } = Array.Empty<BulkEmailRecipientDto>();

    public int InvalidEmailCount { get; init; }
}
