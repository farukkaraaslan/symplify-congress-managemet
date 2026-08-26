using Symplify.Portal.WebUI.Models.PublicSite;

namespace Symplify.Portal.WebUI.Services.PublicSite;

public interface IPublicSiteApiClient
{
    Task<PublicSiteBootstrapResponse> GetBootstrapAsync(string culture, CancellationToken cancellationToken);
    Task<PublicHomeResponse> GetHomeAsync(string culture, CancellationToken cancellationToken);
    Task<PublicBoardsResponse> GetBoardsAsync(string culture, CancellationToken cancellationToken);
    Task<PublicSectionsResponse> GetSectionsAsync(string culture, CancellationToken cancellationToken);
    Task<PublicSectionResponse?> GetSectionByBindingKeyAsync(string bindingKey, string culture, CancellationToken cancellationToken);
    Task<PublicDocumentsResponse> GetDocumentsAsync(string culture, CancellationToken cancellationToken);
    Task<PublicContactResponse> GetContactAsync(string culture, CancellationToken cancellationToken);
    Task<PublicContentsResponse> GetContentsAsync(string culture, CancellationToken cancellationToken);
}
