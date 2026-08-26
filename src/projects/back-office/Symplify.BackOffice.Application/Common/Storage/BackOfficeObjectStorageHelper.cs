using Core.Application.Storage;
using Core.CrossCuttingConcerns.Exceptions.Types;

namespace Symplify.BackOffice.Application.Common.Storage;

public static class BackOfficeObjectStorageHelper
{
    private const long MaxImageSizeInBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".svg"
    };

    public static void ValidateImage(
        string? originalFileName,
        long length,
        bool isRequired,
        string requiredMessage,
        string invalidMessage)
    {
        if (string.IsNullOrWhiteSpace(originalFileName) || length <= 0)
        {
            if (isRequired)
                throw new BusinessException(requiredMessage);

            return;
        }

        if (length > MaxImageSizeInBytes)
            throw new BusinessException(invalidMessage);

        string extension = Path.GetExtension(originalFileName);

        if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
            throw new BusinessException(invalidMessage);
    }

    public static string BuildImageFileName(string prefix, string originalFileName)
    {
        string extension = Path.GetExtension(originalFileName);

        if (string.IsNullOrWhiteSpace(extension))
            extension = ".bin";

        return $"{NormalizeFileNamePart(prefix)}-{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
    }

    public static string BuildObjectName(params string[] segments)
    {
        return string.Join(
            '/',
            segments
                .Where(segment => !string.IsNullOrWhiteSpace(segment))
                .Select(segment => segment.Trim().Trim('/').Replace('\\', '/')));
    }

    public static string NormalizeContentType(string? contentType)
    {
        return string.IsNullOrWhiteSpace(contentType)
            ? "application/octet-stream"
            : contentType.Trim();
    }

    public static bool IsExternalOrLegacyLocalPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        return path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("~/", StringComparison.OrdinalIgnoreCase);
    }

    public static async Task DeleteObjectIfExistsAsync(
        IObjectStorageService objectStorageService,
        string? bucketName,
        string? objectName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(bucketName) ||
            string.IsNullOrWhiteSpace(objectName) ||
            IsExternalOrLegacyLocalPath(objectName))
        {
            return;
        }

        try
        {
            await objectStorageService.DeleteAsync(
                new ObjectStorageDeleteRequest
                {
                    BucketName = bucketName.Trim(),
                    ObjectName = objectName.Trim()
                },
                cancellationToken);
        }
        catch
        {
            // Storage cleanup is best-effort; DB state remains authoritative.
        }
    }

    public static Task<string?> GetReadUrlOrPathAsync(
        IObjectStorageService objectStorageService,
        string? bucketName,
        string? objectName,
        TimeSpan expiresIn,
        CancellationToken cancellationToken)
    {
        _ = objectStorageService;
        _ = expiresIn;
        _ = cancellationToken;

        if (string.IsNullOrWhiteSpace(objectName))
            return Task.FromResult<string?>(null);

        string normalizedObjectName = objectName.Trim().Replace('\\', '/');

        if (string.IsNullOrWhiteSpace(bucketName))
            return Task.FromResult<string?>(IsExternalOrLegacyLocalPath(normalizedObjectName) ? normalizedObjectName : null);

        string normalizedBucketName = bucketName.Trim();

        if (IsExternalUrl(normalizedObjectName))
        {
            return Task.FromResult<string?>(
                TryExtractObjectNameFromStorageUrl(normalizedObjectName, normalizedBucketName, out string? extractedObjectName)
                    ? BuildPublicAssetUrl(normalizedBucketName, extractedObjectName)
                    : normalizedObjectName);
        }

        if (IsLegacyLocalPath(normalizedObjectName))
            return Task.FromResult<string?>(normalizedObjectName);

        return Task.FromResult<string?>(BuildPublicAssetUrl(normalizedBucketName, normalizedObjectName));
    }

    private static bool IsExternalUrl(string path)
        => path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
           path.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    private static bool IsLegacyLocalPath(string path)
        => path.StartsWith("/", StringComparison.OrdinalIgnoreCase) ||
           path.StartsWith("~/", StringComparison.OrdinalIgnoreCase);

    private static bool TryExtractObjectNameFromStorageUrl(
        string url,
        string bucketName,
        out string? objectName)
    {
        objectName = null;

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            return false;

        string path = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/').Replace('\\', '/'));
        string normalizedBucketName = bucketName.Trim().Trim('/');

        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(normalizedBucketName))
            return false;

        string bucketPrefix = normalizedBucketName + "/";

        if (!path.StartsWith(bucketPrefix, StringComparison.Ordinal))
            return false;

        objectName = path[bucketPrefix.Length..].Trim('/');

        return !string.IsNullOrWhiteSpace(objectName);
    }

    private static string BuildPublicAssetUrl(string bucketName, string objectName)
    {
        string encodedBucketName = Uri.EscapeDataString(bucketName.Trim().Trim('/'));
        string encodedObjectName = string.Join(
            '/',
            objectName
                .Trim()
                .Trim('/')
                .Replace('\\', '/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Uri.EscapeDataString));

        return $"/public-assets/{encodedBucketName}/{encodedObjectName}";
    }

    private static string NormalizeFileNamePart(string value)
    {
        string normalized = new string(
            value.Trim().ToLowerInvariant().Select(character =>
                char.IsLetterOrDigit(character) ? character : '-').ToArray());

        while (normalized.Contains("--", StringComparison.Ordinal))
            normalized = normalized.Replace("--", "-", StringComparison.Ordinal);

        return string.IsNullOrWhiteSpace(normalized.Trim('-'))
            ? "file"
            : normalized.Trim('-');
    }
}
