namespace Core.Application.Storage;

public sealed class ObjectStorageFileInfo
{
    public string BucketName { get; init; } = string.Empty;

    public string ObjectName { get; init; } = string.Empty;

    public string? ContentType { get; init; }

    public long Size { get; init; }

    public DateTimeOffset? LastModified { get; init; }

    public string? ETag { get; init; }

    public IDictionary<string, string> Metadata { get; init; }
        = new Dictionary<string, string>();
}
