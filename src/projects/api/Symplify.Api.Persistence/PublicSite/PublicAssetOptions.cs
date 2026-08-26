namespace Symplify.Api.Persistence.PublicSite;

public sealed class PublicAssetOptions
{
    public const string SectionName = "PublicAssets";
    public const string ObjectStorageSectionName = "ObjectStorage";

    /// <summary>
    /// Public base URL of Symplify.Api, for example http://localhost:5200.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Optional prefix for old relative static paths. Usually empty.
    /// </summary>
    public string? RelativePathPrefix { get; set; }

    /// <summary>
    /// Base URL for legacy static files such as /uploads/...
    /// If legacy uploads are still served by BackOffice locally, this can point to BackOffice.
    /// Object storage files must not depend on BackOffice HTTP.
    /// </summary>
    public string? StaticFilesBaseUrl { get; set; }

    /// <summary>
    /// Public URL for object-storage assets exposed by Symplify.Api.
    /// Example: http://localhost:5200/public-assets
    /// </summary>
    public string? ObjectStorageBaseUrl { get; set; }

    public string? ImagesObjectStorageBaseUrl { get; set; }

    public string? DocumentsObjectStorageBaseUrl { get; set; }

    /// <summary>
    /// Kept only for backward compatibility. New public asset flow must read from ObjectStorage directly.
    /// </summary>
    public string? UpstreamPublicAssetsBaseUrl { get; set; }

    public bool AllowInvalidUpstreamCertificate { get; set; }

    /// <summary>
    /// ObjectStorage:Provider. Supported value for now: Minio.
    /// </summary>
    public string Provider { get; set; } = "Minio";

    /// <summary>
    /// ObjectStorage:Endpoint. Can be localhost:9000, http://localhost:9000 or https://host:9000.
    /// </summary>
    public string? Endpoint { get; set; }

    public bool UseSsl { get; set; }

    public string? AccessKey { get; set; }

    public string? SecretKey { get; set; }

    public string Region { get; set; } = "us-east-1";

    public bool PreferDirectObjectStorageForAssets { get; set; } = true;

    public string CongressImagesBucket { get; set; } = "symplify-congress-images";

    public string CongressDocumentsBucket { get; set; } = "symplify-congress-documents";

    public string? SubmissionsBucket { get; set; }

    /// <summary>
    /// Only these public buckets can be served from /public-assets.
    /// Submission/private buckets must not be exposed here.
    /// </summary>
    public ISet<string> AllowedPublicBuckets { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}
