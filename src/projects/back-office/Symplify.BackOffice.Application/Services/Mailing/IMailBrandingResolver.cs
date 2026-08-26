using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Application.Services.Mailing;

public interface IMailBrandingResolver
{
    Task<MailBrandingModel> ResolveForSubmissionAsync(
        Submission submission,
        CancellationToken cancellationToken = default);

    Task<MailBrandingModel> ResolveForCongressAsync(
        Guid congressId,
        Guid? languageId = null,
        string? culture = null,
        CancellationToken cancellationToken = default);

    Task<MailBrandingModel> ResolveForOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    MailBrandingModel ResolveDefault();
}
