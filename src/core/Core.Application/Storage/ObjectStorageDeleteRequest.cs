namespace Core.Application.Storage;

public sealed class ObjectStorageDeleteRequest
{
    public string BucketName { get; init; } = string.Empty;

    public string ObjectName { get; init; } = string.Empty;
}
