using Symplify.Portal.WebUI.Models.PublicSite;

namespace Symplify.Portal.WebUI.Services.PublicSite;

public interface IPublicSiteService
{
    Task<PublicSiteBootstrapResponse> GetShellAsync(CancellationToken cancellationToken);
    Task<PublicHomeResponse> GetHomeAsync(CancellationToken cancellationToken);
    Task<PublicBoardsResponse> GetBoardsAsync(CancellationToken cancellationToken);
    Task<PublicSectionsResponse> GetSectionsAsync(CancellationToken cancellationToken);
    Task<PublicSectionResponse?> GetSectionByBindingKeyAsync(string bindingKey, CancellationToken cancellationToken);
    Task<PublicDocumentsResponse> GetDocumentsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetDocumentTypeNamesAsync(CancellationToken cancellationToken);
    Task<PublicContactResponse> GetContactAsync(CancellationToken cancellationToken);
    Task<PublicContentsResponse> GetContentsAsync(CancellationToken cancellationToken);
}
