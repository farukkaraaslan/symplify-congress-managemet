namespace Core.Application.Storage;

public sealed class ObjectStorageUploadRequest
{
    public string BucketName { get; init; } = string.Empty;

    public string ObjectName { get; init; } = string.Empty;

    public string OriginalFileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = "application/octet-stream";

    public long Size { get; init; }

    public Stream Content { get; init; } = Stream.Null;

    public IDictionary<string, string> Metadata { get; init; }
        = new Dictionary<string, string>();
}
