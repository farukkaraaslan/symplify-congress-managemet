namespace Symplify.BackOffice.Application.Services.Storage;

public interface IObjectStorageRangeReader
{
    Task CopyRangeToAsync(
        string bucketName,
        string objectName,
        Stream destination,
        long offset,
        long length,
        CancellationToken cancellationToken = default);
}
