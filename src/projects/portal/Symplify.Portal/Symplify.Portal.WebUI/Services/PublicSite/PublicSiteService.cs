using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Symplify.Portal.WebUI.Models.PublicSite;
using Symplify.Portal.WebUI.Options;

namespace Symplify.Portal.WebUI.Services.PublicSite;

public sealed class PublicSiteService : IPublicSiteService
{
    private readonly IPublicSiteApiClient _apiClient;
    private readonly IPortalCultureService _cultureService;
    private readonly IMemoryCache _memoryCache;
    private readonly PublicApiOptions _options;

    public PublicSiteService(
        IPublicSiteApiClient apiClient,
        IPortalCultureService cultureService,
        IMemoryCache memoryCache,
        IOptions<PublicApiOptions> options)
    {
        _apiClient = apiClient;
        _cultureService = cultureService;
        _memoryCache = memoryCache;
        _options = options.Value;
    }

    public Task<PublicSiteBootstrapResponse> GetShellAsync(CancellationToken cancellationToken)
    {
        string culture = _cultureService.GetCurrentCulture();
        string cacheKey = $"public-site:shell:{culture}";

        return _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(Math.Max(15, _options.ShellCacheSeconds));
            return await _apiClient.GetBootstrapAsync(culture, cancellationToken);
        })!;
    }

    public Task<PublicHomeResponse> GetHomeAsync(CancellationToken cancellationToken) =>
        _apiClient.GetHomeAsync(_cultureService.GetCurrentCulture(), cancellationToken);

    public Task<PublicBoardsResponse> GetBoardsAsync(CancellationToken cancellationToken) =>
        _apiClient.GetBoardsAsync(_cultureService.GetCurrentCulture(), cancellationToken);

    public Task<PublicSectionsResponse> GetSectionsAsync(CancellationToken cancellationToken) =>
        _apiClient.GetSectionsAsync(_cultureService.GetCurrentCulture(), cancellationToken);

    public Task<PublicSectionResponse?> GetSectionByBindingKeyAsync(string bindingKey, CancellationToken cancellationToken) =>
        _apiClient.GetSectionByBindingKeyAsync(bindingKey, _cultureService.GetCurrentCulture(), cancellationToken);

    public Task<PublicDocumentsResponse> GetDocumentsAsync(CancellationToken cancellationToken) =>
        _apiClient.GetDocumentsAsync(_cultureService.GetCurrentCulture(), cancellationToken);

    public Task<IReadOnlyList<string>> GetDocumentTypeNamesAsync(CancellationToken cancellationToken)
    {
        string culture = _cultureService.GetCurrentCulture();
        string cacheKey = $"public-site:document-types:{culture}";

        return _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(Math.Max(30, _options.ShellCacheSeconds));

            PublicDocumentsResponse documents = await _apiClient.GetDocumentsAsync(culture, cancellationToken);
            return BuildDocumentTypeNames(documents);
        })!;
    }

    public Task<PublicContactResponse> GetContactAsync(CancellationToken cancellationToken) =>
        _apiClient.GetContactAsync(_cultureService.GetCurrentCulture(), cancellationToken);

    public Task<PublicContentsResponse> GetContentsAsync(CancellationToken cancellationToken) =>
        _apiClient.GetContentsAsync(_cultureService.GetCurrentCulture(), cancellationToken);

    private static IReadOnlyList<string> BuildDocumentTypeNames(PublicDocumentsResponse documents)
    {
        List<string> documentTypeNames = new();

        AddDocumentTypeNames(documents.CurrentCongress, documentTypeNames);

        foreach (PublicCongressDocumentGroupResponse archiveCongress in documents.ArchiveCongresses)
        {
            AddDocumentTypeNames(archiveCongress, documentTypeNames);
        }

        return documentTypeNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(name => name)
            .ToArray();
    }

    private static void AddDocumentTypeNames(PublicCongressDocumentGroupResponse? group, ICollection<string> documentTypeNames)
    {
        if (group is null)
        {
            return;
        }

        foreach (PublicDocumentResponse document in group.Documents)
        {
            if (!string.IsNullOrWhiteSpace(document.DocumentTypeName))
            {
                documentTypeNames.Add(document.DocumentTypeName.Trim());
            }
        }
    }
}
