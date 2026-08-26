namespace Core.Application.Storage;

public sealed class ObjectStorageUploadResult
{
    public string BucketName { get; init; } = string.Empty;

    public string ObjectName { get; init; } = string.Empty;

    public string OriginalFileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = "application/octet-stream";

    public long Size { get; init; }

    public string? ETag { get; init; }

    public string? PublicUrl { get; init; }
}
