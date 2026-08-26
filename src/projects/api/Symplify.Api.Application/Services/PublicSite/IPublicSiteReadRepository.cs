using Symplify.Api.Application.Features.PublicSite.Responses;

namespace Symplify.Api.Application.Services.PublicSite;

public interface IPublicSiteReadRepository
{
    Task<PublicSiteBootstrapResponse> GetBootstrapAsync(Guid organizationId, string? culture, CancellationToken cancellationToken);
    Task<PublicHomeResponse> GetHomeAsync(Guid organizationId, string? culture, CancellationToken cancellationToken);
    Task<PublicBoardsResponse> GetBoardsAsync(Guid organizationId, string? culture, CancellationToken cancellationToken);
    Task<PublicSectionsResponse> GetSectionsAsync(Guid organizationId, string? culture, CancellationToken cancellationToken);
    Task<PublicSectionResponse?> GetSectionByBindingKeyAsync(Guid organizationId, string bindingKey, string? culture, CancellationToken cancellationToken);
    Task<PublicDocumentsResponse> GetDocumentsAsync(Guid organizationId, string? culture, CancellationToken cancellationToken);
    Task<PublicContactResponse> GetContactAsync(Guid organizationId, string? culture, CancellationToken cancellationToken);
    Task<PublicContentsResponse> GetContentsAsync(Guid organizationId, string? culture, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, string>> GetLocalizationResourcesAsync(string? culture, CancellationToken cancellationToken);
}
