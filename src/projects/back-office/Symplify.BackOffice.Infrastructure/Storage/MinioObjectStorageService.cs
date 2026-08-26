using Core.Application.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using Symplify.BackOffice.Application.Services.Storage;

namespace Symplify.BackOffice.Infrastructure.Storage;

public sealed class MinioObjectStorageService : IObjectStorageService, IObjectStoragePrefixCleanupService
{
    private const int MinimumPresignedExpirySeconds = 1;
    private const int MaximumPresignedExpirySeconds = 60 * 60 * 24 * 7;

    private readonly IMinioClient _minioClient;
    private readonly ObjectStorageOptions _options;
    private readonly ILogger<MinioObjectStorageService> _logger;

    public MinioObjectStorageService(
        IMinioClient minioClient,
        IOptions<ObjectStorageOptions> options,
        ILogger<MinioObjectStorageService> logger)
    {
        _minioClient = minioClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ObjectStorageUploadResult> UploadAsync(
        ObjectStorageUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateUploadRequest(request);

        PutObjectArgs putObjectArgs = new PutObjectArgs()
            .WithBucket(request.BucketName)
            .WithObject(request.ObjectName)
            .WithStreamData(request.Content)
            .WithObjectSize(request.Size)
            .WithContentType(NormalizeContentType(request.ContentType));

        if (request.Metadata.Count > 0)
        {
            putObjectArgs.WithHeaders(NormalizeMetadata(request.Metadata));
        }

        await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);

        ObjectStorageFileInfo? fileInfo = await GetFileInfoAsync(
            request.BucketName,
            request.ObjectName,
            cancellationToken);

        return new ObjectStorageUploadResult
        {
            BucketName = request.BucketName,
            ObjectName = request.ObjectName,
            OriginalFileName = request.OriginalFileName,
            ContentType = fileInfo?.ContentType ?? NormalizeContentType(request.ContentType),
            Size = fileInfo?.Size > 0 ? fileInfo.Size : request.Size,
            ETag = fileInfo?.ETag,
            PublicUrl = null
        };
    }
    public async Task DeleteAsync(
        ObjectStorageDeleteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateBucketAndObject(request.BucketName, request.ObjectName);

        try
        {
            RemoveObjectArgs removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(request.BucketName)
                .WithObject(request.ObjectName);

            await _minioClient.RemoveObjectAsync(removeObjectArgs, cancellationToken);
        }
        catch (MinioException exception) when (IsNotFound(exception))
        {
            _logger.LogDebug(
                exception,
                "Object already missing while deleting object storage item. Bucket: {BucketName}, Object: {ObjectName}",
                request.BucketName,
                request.ObjectName);
        }
    }



    public async Task<IReadOnlyList<string>> DeletePrefixAsync(
        string bucketName,
        string prefix,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bucketName) || string.IsNullOrWhiteSpace(prefix))
            return Array.Empty<string>();

        string normalizedBucketName = bucketName.Trim();
        string normalizedPrefix = prefix.Trim().TrimStart('/').Replace('\\', '/');

        if (string.IsNullOrWhiteSpace(normalizedPrefix))
            return Array.Empty<string>();

        List<string> objectNames = new();

        ListObjectsArgs listArgs = new ListObjectsArgs()
            .WithBucket(normalizedBucketName)
            .WithPrefix(normalizedPrefix)
            .WithRecursive(true);

        await foreach (var item in _minioClient.ListObjectsEnumAsync(listArgs, cancellationToken))
        {
            if (!string.IsNullOrWhiteSpace(item.Key))
                objectNames.Add(item.Key);
        }

        List<string> deletedObjectNames = new();

        foreach (string objectName in objectNames.Distinct(StringComparer.Ordinal))
        {
            await DeleteAsync(
                new ObjectStorageDeleteRequest
                {
                    BucketName = normalizedBucketName,
                    ObjectName = objectName
                },
                cancellationToken);

            deletedObjectNames.Add(objectName);
        }

        return deletedObjectNames;
    }

    public async Task<bool> ExistsAsync(
        string bucketName,
        string objectName,
        CancellationToken cancellationToken = default)
    {
        ValidateBucketAndObject(bucketName, objectName);

        try
        {
            StatObjectArgs statObjectArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            await _minioClient.StatObjectAsync(statObjectArgs, cancellationToken);

            return true;
        }
        catch (MinioException exception) when (IsNotFound(exception))
        {
            return false;
        }
    }

    public async Task<ObjectStorageFileInfo?> GetFileInfoAsync(
        string bucketName,
        string objectName,
        CancellationToken cancellationToken = default)
    {
        ValidateBucketAndObject(bucketName, objectName);

        try
        {
            StatObjectArgs statObjectArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            object stat = await _minioClient.StatObjectAsync(statObjectArgs, cancellationToken);

            return new ObjectStorageFileInfo
            {
                BucketName = bucketName,
                ObjectName = objectName,
                ContentType = GetStringProperty(stat, "ContentType"),
                Size = GetLongProperty(stat, "Size"),
                LastModified = GetDateTimeOffsetProperty(stat, "LastModified"),
                ETag = GetStringProperty(stat, "ETag") ?? GetStringProperty(stat, "Etag"),
                Metadata = GetMetadata(stat)
            };
        }
        catch (MinioException exception) when (IsNotFound(exception))
        {
            return null;
        }
    }

    public async Task<Stream> OpenReadAsync(
        string bucketName,
        string objectName,
        CancellationToken cancellationToken = default)
    {
        ValidateBucketAndObject(bucketName, objectName);

        MemoryStream memoryStream = new();

        try
        {
            GetObjectArgs getObjectArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream));

            await _minioClient.GetObjectAsync(getObjectArgs, cancellationToken: cancellationToken);

            memoryStream.Position = 0;

            return memoryStream;
        }
        catch
        {
            await memoryStream.DisposeAsync();
            throw;
        }
    }

    public Task<string> GetPresignedReadUrlAsync(
        string bucketName,
        string objectName,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default)
    {
        ValidateBucketAndObject(bucketName, objectName);

        int expirySeconds = NormalizePresignedExpiry(expiresIn);

        PresignedGetObjectArgs args = new PresignedGetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithExpiry(expirySeconds);

        return _minioClient.PresignedGetObjectAsync(args);
    }

    private async Task EnsureBucketExistsAsync(
        string bucketName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
            throw new InvalidOperationException("Object storage bucket name is required.");

        BucketExistsArgs bucketExistsArgs = new BucketExistsArgs()
            .WithBucket(bucketName);

        bool exists = await _minioClient.BucketExistsAsync(
            bucketExistsArgs,
            cancellationToken);

        if (exists)
            return;

        MakeBucketArgs makeBucketArgs = new MakeBucketArgs()
            .WithBucket(bucketName);

        await _minioClient.MakeBucketAsync(
            makeBucketArgs,
            cancellationToken);
    }

    private static void ValidateUploadRequest(ObjectStorageUploadRequest request)
    {
        ValidateBucketAndObject(request.BucketName, request.ObjectName);

        if (request.Content == Stream.Null)
            throw new InvalidOperationException("Object storage upload content stream is required.");

        if (request.Size < 0)
            throw new InvalidOperationException("Object storage upload size cannot be negative.");
    }

    private static void ValidateBucketAndObject(string bucketName, string objectName)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
            throw new InvalidOperationException("Object storage bucket name is required.");

        if (string.IsNullOrWhiteSpace(objectName))
            throw new InvalidOperationException("Object storage object name is required.");
    }

    private static string NormalizeContentType(string? contentType)
    {
        return string.IsNullOrWhiteSpace(contentType)
            ? "application/octet-stream"
            : contentType.Trim();
    }

    private static Dictionary<string, string> NormalizeMetadata(
        IDictionary<string, string> metadata)
    {
        Dictionary<string, string> normalizedMetadata = new(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, string> item in metadata)
        {
            if (string.IsNullOrWhiteSpace(item.Key) || item.Value is null)
                continue;

            string key = NormalizeMetadataKey(item.Key);

            if (string.IsNullOrWhiteSpace(key))
                continue;

            if (!key.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase))
                key = $"x-amz-meta-{key}";

            normalizedMetadata[key] = NormalizeMetadataValue(item.Value);
        }

        return normalizedMetadata;
    }

    private static string NormalizeMetadataKey(string key)
    {
        string normalizedKey = key.Trim();

        if (normalizedKey.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase))
            normalizedKey = normalizedKey["x-amz-meta-".Length..];

        char[] chars = normalizedKey
            .Where(character =>
                character is >= 'a' and <= 'z' ||
                character is >= 'A' and <= 'Z' ||
                character is >= '0' and <= '9' ||
                character == '-' ||
                character == '_')
            .ToArray();

        return new string(chars).Trim('-', '_');
    }

    private static string NormalizeMetadataValue(string value)
    {
        string normalizedValue = value.Trim();

        if (IsAsciiHeaderValue(normalizedValue))
            return normalizedValue;

        return Uri.EscapeDataString(normalizedValue);
    }

    private static bool IsAsciiHeaderValue(string value)
    {
        foreach (char character in value)
        {
            if (character is < ' ' or > '~')
                return false;
        }

        return true;
    }

    private static int NormalizePresignedExpiry(TimeSpan expiresIn)
    {
        if (expiresIn <= TimeSpan.Zero)
            return MinimumPresignedExpirySeconds;

        double totalSeconds = Math.Ceiling(expiresIn.TotalSeconds);

        if (totalSeconds > MaximumPresignedExpirySeconds)
            return MaximumPresignedExpirySeconds;

        return Math.Max(MinimumPresignedExpirySeconds, Convert.ToInt32(totalSeconds));
    }

    private static bool IsNotFound(MinioException exception)
    {
        string exceptionType = exception.GetType().Name;

        if (exceptionType.Contains("ObjectNotFound", StringComparison.OrdinalIgnoreCase) ||
            exceptionType.Contains("BucketNotFound", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string message = exception.Message ?? string.Empty;

        return message.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("no such key", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("specified key does not exist", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("NoSuchKey", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetStringProperty(object source, string propertyName)
    {
        object? value = source.GetType().GetProperty(propertyName)?.GetValue(source);

        return value?.ToString();
    }

    private static long GetLongProperty(object source, string propertyName)
    {
        object? value = source.GetType().GetProperty(propertyName)?.GetValue(source);

        if (value is null)
            return 0;

        return Convert.ToInt64(value);
    }

    private static DateTimeOffset? GetDateTimeOffsetProperty(
        object source,
        string propertyName)
    {
        object? value = source.GetType().GetProperty(propertyName)?.GetValue(source);

        return value switch
        {
            DateTimeOffset dateTimeOffset => dateTimeOffset,
            DateTime dateTime => new DateTimeOffset(dateTime),
            _ => null
        };
    }

    private static IDictionary<string, string> GetMetadata(object stat)
    {
        object? metadataValue = stat.GetType().GetProperty("MetaData")?.GetValue(stat) ??
                                stat.GetType().GetProperty("Metadata")?.GetValue(stat);

        if (metadataValue is IDictionary<string, string> stringDictionary)
            return new Dictionary<string, string>(stringDictionary);

        if (metadataValue is not System.Collections.IDictionary dictionary)
            return new Dictionary<string, string>();

        Dictionary<string, string> metadata = new(StringComparer.OrdinalIgnoreCase);

        foreach (System.Collections.DictionaryEntry entry in dictionary)
        {
            if (entry.Key is null || entry.Value is null)
                continue;

            metadata[entry.Key.ToString() ?? string.Empty] = entry.Value.ToString() ?? string.Empty;
        }

        return metadata;
    }
}
