using Core.Application.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Symplify.BackOffice.WebUI.Controllers;

[AllowAnonymous]
[Route("public-assets")]
public sealed class PublicAssetsController : Controller
{
    private static readonly HashSet<string> AllowedPublicSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        "logos",
        "sliders",
        "content-assets",
        "congress-content-assets",
        "profile-photos"
    };

    private static readonly HashSet<string> BlockedPrivateSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        "signature",
        "signatures",
        "acceptance-letters",
        "submissions",
        "private-documents"
    };

    private static readonly HashSet<string> AllowedPublicContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "text/plain"
    };

    private readonly IObjectStorageService _objectStorageService;
    private readonly ObjectStorageOptions _storageOptions;

    public PublicAssetsController(
        IObjectStorageService objectStorageService,
        IOptions<ObjectStorageOptions> storageOptions)
    {
        _objectStorageService = objectStorageService;
        _storageOptions = storageOptions.Value;
    }

    [HttpGet("{bucketName}/{**objectName}")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Get(string bucketName, string objectName, CancellationToken cancellationToken)
    {
        string requestedBucketName = Normalize(bucketName) ?? string.Empty;
        string requestedObjectName = NormalizeObjectName(objectName) ?? string.Empty;

        if (!IsAllowedPublicBucket(requestedBucketName))
            return NotFound();

        if (!IsSafePublicObjectName(requestedBucketName, requestedObjectName))
            return NotFound();

        ObjectStorageFileInfo? fileInfo = await _objectStorageService.GetFileInfoAsync(
            requestedBucketName,
            requestedObjectName,
            cancellationToken);

        if (fileInfo is null)
            return NotFound();

        string contentType = ResolveContentType(fileInfo.ContentType, requestedObjectName);
        if (!AllowedPublicContentTypes.Contains(contentType))
            return NotFound();

        if (IsCongressDocumentCoverObjectName(requestedBucketName, requestedObjectName) && !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return NotFound();

        Stream stream = await _objectStorageService.OpenReadAsync(
            requestedBucketName,
            requestedObjectName,
            cancellationToken);

        Response.Headers.CacheControl = "public,max-age=86400";
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        return File(stream, contentType);
    }

    private bool IsAllowedPublicBucket(string requestedBucketName)
    {
        string? congressImagesBucketName = Normalize(_storageOptions.Buckets.CongressImages);
        string? congressDocumentsBucketName = Normalize(_storageOptions.Buckets.CongressDocuments);

        return string.Equals(requestedBucketName, congressImagesBucketName, StringComparison.Ordinal) ||
               string.Equals(requestedBucketName, congressDocumentsBucketName, StringComparison.Ordinal);
    }

    private bool IsSafePublicObjectName(string requestedBucketName, string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName) ||
            objectName.StartsWith("/", StringComparison.Ordinal) ||
            objectName.Contains('\\') ||
            objectName.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        string[] segments = objectName
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length == 0)
            return false;

        if (segments.Any(segment => BlockedPrivateSegments.Contains(segment)))
            return false;

        if (segments.Any(segment => AllowedPublicSegments.Contains(segment)))
            return true;

        return IsCongressDocumentCoverObjectName(requestedBucketName, objectName);
    }

    private bool IsCongressDocumentCoverObjectName(string requestedBucketName, string objectName)
    {
        string? congressImagesBucketName = Normalize(_storageOptions.Buckets.CongressImages);
        if (!string.Equals(requestedBucketName, congressImagesBucketName, StringComparison.Ordinal))
            return false;

        string[] segments = objectName
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return segments.Length == 7 &&
               string.Equals(segments[0], "backoffice", StringComparison.OrdinalIgnoreCase) &&
               string.Equals(segments[1], "congresses", StringComparison.OrdinalIgnoreCase) &&
               Guid.TryParse(segments[2], out _) &&
               string.Equals(segments[3], "documents", StringComparison.OrdinalIgnoreCase) &&
               Guid.TryParse(segments[4], out _) &&
               string.Equals(segments[5], "cover", StringComparison.OrdinalIgnoreCase) &&
               IsAllowedImageFileName(segments[6]);
    }

    private static bool IsAllowedImageFileName(string fileName)
    {
        string extension = Path.GetExtension(fileName);
        return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveContentType(string? contentType, string objectName)
    {
        if (!string.IsNullOrWhiteSpace(contentType) && AllowedPublicContentTypes.Contains(contentType.Trim()))
            return contentType.Trim();

        string extension = Path.GetExtension(objectName);

        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeObjectName(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : Uri.UnescapeDataString(value.Trim().Replace('\\', '/'));
}
