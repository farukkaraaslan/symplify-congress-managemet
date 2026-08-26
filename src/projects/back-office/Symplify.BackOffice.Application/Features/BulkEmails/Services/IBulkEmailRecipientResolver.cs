using Symplify.BackOffice.Application.Features.BulkEmails.Dtos;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.BulkEmails.Services;

public interface IBulkEmailRecipientResolver
{
    Task<BulkEmailRecipientResolutionResult> ResolveAsync(
        Guid congressId,
        BulkEmailAudienceType audienceType,
        CancellationToken cancellationToken = default);

    Task<BulkEmailRecipientResolutionResult> ResolveAdjustedAsync(
        Guid congressId,
        BulkEmailAudienceType audienceType,
        IReadOnlyCollection<string>? excludedRecipientEmails,
        IReadOnlyCollection<BulkEmailRecipientDto>? additionalRecipients,
        CancellationToken cancellationToken = default);
}
