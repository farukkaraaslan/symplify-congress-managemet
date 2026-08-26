namespace Core.Application.Storage;

public sealed class ObjectStorageOptions
{
    public const string SectionName = "ObjectStorage";

    public string Provider { get; set; } = "Minio";

    public string Endpoint { get; set; } = string.Empty;

    public bool UseSsl { get; set; }

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public ObjectStorageBucketOptions Buckets { get; set; } = new();
}
