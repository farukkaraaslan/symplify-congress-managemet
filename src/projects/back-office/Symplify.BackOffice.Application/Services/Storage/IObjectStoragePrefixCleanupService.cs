namespace Symplify.BackOffice.Application.Services.Storage;

public interface IObjectStoragePrefixCleanupService
{
    Task<IReadOnlyList<string>> DeletePrefixAsync(
        string bucketName,
        string prefix,
        CancellationToken cancellationToken = default);
}
