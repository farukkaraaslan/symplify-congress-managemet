namespace Symplify.BackOffice.WebUI.Localization;

public interface IClientResourceService
{
    Task<IReadOnlyDictionary<string, string>> GetResourcesAsync(
        IEnumerable<string> prefixes,
        CancellationToken cancellationToken = default);
}
