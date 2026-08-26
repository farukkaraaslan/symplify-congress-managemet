using System.Net;
using System.Net.Http.Headers;
using Minio;
using Minio.DataModel.Args;
using Symplify.BackOffice.Application.Services.Storage;

namespace Symplify.BackOffice.Infrastructure.Storage;

public sealed class MinioObjectStorageRangeReader : IObjectStorageRangeReader
{
    private const int CopyBufferSize = 1024 * 64;
    private const int PresignedReadExpirySeconds = 60 * 5;

    private static readonly HttpClient InternalObjectHttpClient = new(
        new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.None,
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = 128
        })
    {
        Timeout = Timeout.InfiniteTimeSpan
    };

    private readonly IMinioClient _minioClient;

    public MinioObjectStorageRangeReader(IMinioClient minioClient)
    {
        _minioClient = minioClient;
    }

    public async Task CopyRangeToAsync(
        string bucketName,
        string objectName,
        Stream destination,
        long offset,
        long length,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bucketName);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectName);
        ArgumentNullException.ThrowIfNull(destination);

        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset), "Object range offset cannot be negative.");

        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Object range length must be greater than zero.");

        string normalizedBucketName = bucketName.Trim();
        string normalizedObjectName = objectName.Trim();

        string internalReadUrl = await CreateInternalPresignedReadUrlAsync(
            normalizedBucketName,
            normalizedObjectName,
            cancellationToken);

        using HttpRequestMessage request = new(HttpMethod.Get, internalReadUrl);
        request.Headers.Range = new RangeHeaderValue(offset, offset + length - 1);

        using HttpResponseMessage response = await InternalObjectHttpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (response.StatusCode != HttpStatusCode.PartialContent && response.StatusCode != HttpStatusCode.OK)
        {
            response.EnsureSuccessStatusCode();
        }

        long? responseLength = response.Content.Headers.ContentLength;
        if (responseLength.HasValue && responseLength.Value > length)
        {
            throw new InvalidOperationException(
                $"Object storage returned more bytes than requested. Bucket: {normalizedBucketName}, Object: {normalizedObjectName}, Requested: {length}, Returned: {responseLength.Value}.");
        }

        await using Stream source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await source.CopyToAsync(destination, CopyBufferSize, cancellationToken);
    }

    private Task<string> CreateInternalPresignedReadUrlAsync(
        string bucketName,
        string objectName,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        PresignedGetObjectArgs args = new PresignedGetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithExpiry(PresignedReadExpirySeconds);

        return _minioClient.PresignedGetObjectAsync(args);
    }
}
