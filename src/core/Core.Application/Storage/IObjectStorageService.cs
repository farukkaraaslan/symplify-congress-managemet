namespace Core.Application.Storage;

public interface IObjectStorageService
{
    Task<ObjectStorageUploadResult> UploadAsync(
        ObjectStorageUploadRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        ObjectStorageDeleteRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string bucketName,
        string objectName,
        CancellationToken cancellationToken = default);

    Task<ObjectStorageFileInfo?> GetFileInfoAsync(
        string bucketName,
        string objectName,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(
        string bucketName,
        string objectName,
        CancellationToken cancellationToken = default);

    Task<string> GetPresignedReadUrlAsync(
        string bucketName,
        string objectName,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default);
}
