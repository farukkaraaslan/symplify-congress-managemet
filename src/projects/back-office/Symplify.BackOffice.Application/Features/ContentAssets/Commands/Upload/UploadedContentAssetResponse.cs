namespace Symplify.BackOffice.Application.Features.ContentAssets.Commands.Upload;

public sealed class UploadedContentAssetResponse
{
    public string BucketName { get; set; } = string.Empty;
    public string ObjectName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string? FileExtension { get; set; }
    public long FileSize { get; set; }
    public string? ETag { get; set; }
}
