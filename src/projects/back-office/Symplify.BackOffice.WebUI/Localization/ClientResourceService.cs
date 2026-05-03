namespace Symplify.BackOffice.WebUI.Localization;

public sealed class ClientResourceService : IClientResourceService
{
    private readonly IBackOfficeResourceProvider _resourceProvider;

    public ClientResourceService(IBackOfficeResourceProvider resourceProvider)
    {
        _resourceProvider = resourceProvider;
    }

    public Task<IReadOnlyDictionary<string, string>> GetResourcesAsync(
        IEnumerable<string> prefixes,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string culture = _resourceProvider.ResolveCurrentCulture();
        IReadOnlyDictionary<string, string> resources = _resourceProvider.GetResourcesByPrefixes(prefixes, culture);

        return Task.FromResult(resources);
    }
}
