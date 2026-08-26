using Microsoft.Extensions.Options;

namespace Symplify.Api.Persistence.PublicSite;

public sealed class PublicAssetUrlBuilder : IPublicAssetUrlBuilder
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".apng", ".avif", ".gif", ".jpg", ".jpeg", ".png", ".svg", ".webp", ".bmp"
    };

    private readonly PublicAssetOptions _options;

    public PublicAssetUrlBuilder(IOptions<PublicAssetOptions> options)
    {
        _options = options.Value;
    }

    public string? Build(string? pathOrObjectName, string? bucketName = null)
    {
        if (string.IsNullOrWhiteSpace(pathOrObjectName))
            return null;

        string value = NormalizeSeparators(pathOrObjectName.Trim());

        if (Uri.TryCreate(value, UriKind.Absolute, out _))
            return value;

        string relativeValue = value.TrimStart('/');

        if (IsStaticRelativePath(relativeValue))
            return BuildStaticFileUrl(relativeValue);

        if (relativeValue.StartsWith("public-assets/", StringComparison.OrdinalIgnoreCase))
            return BuildFromBaseUrl(relativeValue);

        string? resolvedBucket = FirstNonEmpty(bucketName, GuessBucket(relativeValue));
        string? objectStorageBaseUrl = ResolveObjectStorageBaseUrl(resolvedBucket, relativeValue);

        if (!string.IsNullOrWhiteSpace(objectStorageBaseUrl) && !string.IsNullOrWhiteSpace(resolvedBucket))
            return CombineUrl(objectStorageBaseUrl, resolvedBucket, relativeValue);

        return BuildFromBaseUrl(relativeValue);
    }

    private string? BuildStaticFileUrl(string relativeValue)
    {
        string relativePrefix = NormalizeRelativePrefix(_options.RelativePathPrefix);
        string relativePath = CombinePath(relativePrefix, relativeValue);
        string? staticBaseUrl = FirstNonEmpty(_options.StaticFilesBaseUrl, _options.BaseUrl);

        if (string.IsNullOrWhiteSpace(staticBaseUrl))
            return relativePath.StartsWith('/') ? relativePath : $"/{relativePath}";

        return CombineUrl(staticBaseUrl, relativePath);
    }

    private string? BuildFromBaseUrl(string relativeValue)
    {
        string relativePrefix = NormalizeRelativePrefix(_options.RelativePathPrefix);
        string relativePath = CombinePath(relativePrefix, relativeValue);

        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            return relativePath.StartsWith('/') ? relativePath : $"/{relativePath}";

        return CombineUrl(_options.BaseUrl, relativePath);
    }

    private string? ResolveObjectStorageBaseUrl(string? bucketName, string relativeValue)
    {
        if (IsImageBucket(bucketName) || IsImagePath(relativeValue))
            return FirstNonEmpty(_options.ImagesObjectStorageBaseUrl, _options.ObjectStorageBaseUrl, BuildDefaultObjectStorageBaseUrl());

        if (IsDocumentBucket(bucketName) || !IsImagePath(relativeValue))
            return FirstNonEmpty(_options.DocumentsObjectStorageBaseUrl, _options.ObjectStorageBaseUrl, BuildDefaultObjectStorageBaseUrl());

        return FirstNonEmpty(_options.ObjectStorageBaseUrl, BuildDefaultObjectStorageBaseUrl());
    }

    private string? BuildDefaultObjectStorageBaseUrl()
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            return null;

        return CombineUrl(_options.BaseUrl, "public-assets");
    }

    private string? GuessBucket(string relativeValue)
    {
        string extension = Path.GetExtension(relativeValue);

        if (string.IsNullOrWhiteSpace(extension))
            return null;

        return ImageExtensions.Contains(extension)
            ? _options.CongressImagesBucket
            : _options.CongressDocumentsBucket;
    }

    private bool IsImageBucket(string? bucketName)
    {
        return !string.IsNullOrWhiteSpace(bucketName)
            && string.Equals(bucketName.Trim(), _options.CongressImagesBucket, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsDocumentBucket(string? bucketName)
    {
        return !string.IsNullOrWhiteSpace(bucketName)
            && string.Equals(bucketName.Trim(), _options.CongressDocumentsBucket, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsImagePath(string relativeValue)
    {
        string extension = Path.GetExtension(relativeValue);
        return !string.IsNullOrWhiteSpace(extension) && ImageExtensions.Contains(extension);
    }

    private static bool IsStaticRelativePath(string relativeValue)
    {
        return relativeValue.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase)
            || relativeValue.StartsWith("assets/", StringComparison.OrdinalIgnoreCase)
            || relativeValue.StartsWith("images/", StringComparison.OrdinalIgnoreCase)
            || relativeValue.StartsWith("img/", StringComparison.OrdinalIgnoreCase)
            || relativeValue.StartsWith("files/", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeSeparators(string value)
    {
        return value.Replace('\\', '/');
    }

    private static string NormalizeRelativePrefix(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim().Trim('/');
    }

    private static string CombinePath(string prefix, string value)
    {
        string cleanedValue = value.Trim().TrimStart('/');

        if (string.IsNullOrWhiteSpace(prefix))
            return cleanedValue;

        return $"{prefix}/{cleanedValue}";
    }

    private static string CombineUrl(params string?[] segments)
    {
        string[] cleanedSegments = segments
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .Select((segment, index) => index == 0 ? segment!.Trim().TrimEnd('/') : segment!.Trim().Trim('/'))
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .ToArray();

        return string.Join('/', cleanedSegments);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (string? value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
    }
}
