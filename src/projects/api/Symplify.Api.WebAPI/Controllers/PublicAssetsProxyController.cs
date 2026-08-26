using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Symplify.Api.Persistence.PublicSite;

namespace Symplify.Api.WebAPI.Controllers;

[ApiController]
[Route("public-assets")]
public sealed class PublicAssetsProxyController : ControllerBase
{
    private const string S3ServiceName = "s3";
    private const string Aws4Request = "aws4_request";
    private const string EmptyPayloadSha256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    private static readonly HashSet<string> InlineExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".apng", ".avif", ".gif", ".jpg", ".jpeg", ".png", ".svg", ".webp", ".bmp"
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PublicAssetOptions _options;
    private readonly ILogger<PublicAssetsProxyController> _logger;

    public PublicAssetsProxyController(
        IHttpClientFactory httpClientFactory,
        IOptions<PublicAssetOptions> options,
        ILogger<PublicAssetsProxyController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    [HttpGet("{bucketName}/{**objectName}")]
    public async Task<IActionResult> GetAsync(
        string bucketName,
        string objectName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(bucketName) || string.IsNullOrWhiteSpace(objectName))
            return AssetProblemPage(
                StatusCodes.Status400BadRequest,
                "Geçersiz belge bağlantısı",
                "Açmaya çalıştığınız belge bağlantısı geçerli değil.");

        string normalizedBucketName = bucketName.Trim().Trim('/');
        string normalizedObjectName = NormalizeObjectName(objectName);

        if (ContainsUnsafePathSegment(normalizedObjectName))
        {
            _logger.LogWarning(
                "Rejected unsafe public asset object name. Bucket: {BucketName}, ObjectName: {ObjectName}",
                normalizedBucketName,
                normalizedObjectName);

            return AssetProblemPage(
                StatusCodes.Status400BadRequest,
                "Geçersiz belge bağlantısı",
                "Açmaya çalıştığınız belge bağlantısı güvenli değil.");
        }

        if (!IsAllowedPublicBucket(normalizedBucketName))
        {
            _logger.LogWarning(
                "Rejected non-public bucket request. Bucket: {BucketName}, ObjectName: {ObjectName}",
                normalizedBucketName,
                normalizedObjectName);

            return AssetProblemPage(
                StatusCodes.Status404NotFound,
                "Belge bulunamadı",
                "Açmaya çalıştığınız belge yayında değil veya erişime açık değil.");
        }

        if (!_options.PreferDirectObjectStorageForAssets)
        {
            _logger.LogWarning("Direct object-storage public asset serving is disabled by configuration.");

            return AssetProblemPage(
                StatusCodes.Status503ServiceUnavailable,
                "Belge servisine ulaşılamıyor",
                "Belge servisi şu anda yapılandırılmamış veya geçici olarak kullanılamıyor.");
        }

        if (!HasObjectStorageConfiguration())
        {
            _logger.LogWarning(
                "ObjectStorage configuration is missing. Endpoint configured: {EndpointConfigured}, AccessKey configured: {AccessKeyConfigured}, SecretKey configured: {SecretKeyConfigured}",
                !string.IsNullOrWhiteSpace(_options.Endpoint),
                !string.IsNullOrWhiteSpace(_options.AccessKey),
                !string.IsNullOrWhiteSpace(_options.SecretKey));

            return AssetProblemPage(
                StatusCodes.Status503ServiceUnavailable,
                "Belge servisine ulaşılamıyor",
                "Belge servisi şu anda yapılandırılmamış veya geçici olarak kullanılamıyor.");
        }

        string objectStorageEndpoint = NormalizeObjectStorageEndpoint(_options.Endpoint!, _options.UseSsl);
        string objectStorageObjectUrl = BuildObjectStorageObjectUrl(objectStorageEndpoint, normalizedBucketName, normalizedObjectName);
        HttpClient httpClient = _httpClientFactory.CreateClient("PublicAssetsProxy");

        try
        {
            using HttpRequestMessage objectStorageRequest = CreateSignedS3GetRequest(
                objectStorageObjectUrl,
                _options.AccessKey!,
                _options.SecretKey!,
                FirstNonEmpty(_options.Region, "us-east-1")!);

            using HttpResponseMessage objectStorageResponse = await httpClient.SendAsync(
                objectStorageRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (objectStorageResponse.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogInformation(
                    "Public asset was not found in object storage. Bucket: {BucketName}, ObjectName: {ObjectName}, ObjectStorageUrl: {ObjectStorageUrl}",
                    normalizedBucketName,
                    normalizedObjectName,
                    objectStorageObjectUrl);

                return AssetProblemPage(
                    StatusCodes.Status404NotFound,
                    "Belge bulunamadı",
                    "Açmaya çalıştığınız belge yayından kaldırılmış, taşınmış veya henüz yayınlanmamış olabilir.");
            }

            if (objectStorageResponse.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning(
                    "Object storage rejected public asset request. Bucket: {BucketName}, ObjectName: {ObjectName}, StatusCode: {StatusCode}",
                    normalizedBucketName,
                    normalizedObjectName,
                    (int)objectStorageResponse.StatusCode);

                return AssetProblemPage(
                    StatusCodes.Status503ServiceUnavailable,
                    "Belge şu anda indirilemiyor",
                    "Belge servisi yetkilendirme nedeniyle yanıt vermedi. Lütfen sistem yöneticisiyle iletişime geçiniz.");
            }

            if (!objectStorageResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Object storage public asset request failed. Bucket: {BucketName}, ObjectName: {ObjectName}, StatusCode: {StatusCode}",
                    normalizedBucketName,
                    normalizedObjectName,
                    (int)objectStorageResponse.StatusCode);

                return AssetProblemPage(
                    StatusCodes.Status503ServiceUnavailable,
                    "Belge şu anda indirilemiyor",
                    "Belge servisi geçici olarak yanıt vermiyor. Lütfen daha sonra tekrar deneyiniz.");
            }

            return await WriteAssetResponseAsync(objectStorageResponse, normalizedObjectName, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Object storage public asset request timed out. Bucket: {BucketName}, ObjectName: {ObjectName}",
                normalizedBucketName,
                normalizedObjectName);

            return AssetProblemPage(
                StatusCodes.Status503ServiceUnavailable,
                "Belge şu anda indirilemiyor",
                "Belge servisi zamanında yanıt vermedi. Lütfen daha sonra tekrar deneyiniz.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(
                exception,
                "Object storage public asset request could not reach storage. Bucket: {BucketName}, ObjectName: {ObjectName}, Endpoint: {Endpoint}",
                normalizedBucketName,
                normalizedObjectName,
                _options.Endpoint);

            return AssetProblemPage(
                StatusCodes.Status503ServiceUnavailable,
                "Belge şu anda indirilemiyor",
                "Belge servisine şu anda ulaşılamıyor. Lütfen daha sonra tekrar deneyiniz.");
        }
        catch (UriFormatException exception)
        {
            _logger.LogWarning(
                exception,
                "Object storage endpoint is invalid. Endpoint: {Endpoint}",
                _options.Endpoint);

            return AssetProblemPage(
                StatusCodes.Status503ServiceUnavailable,
                "Belge servisi yapılandırması geçersiz",
                "Belge servisi adresi geçersiz yapılandırılmış. Lütfen sistem yöneticisiyle iletişime geçiniz.");
        }
    }

    private async Task<IActionResult> WriteAssetResponseAsync(
        HttpResponseMessage sourceResponse,
        string normalizedObjectName,
        CancellationToken cancellationToken)
    {
        string fileName = Path.GetFileName(normalizedObjectName);
        string extension = Path.GetExtension(fileName);
        string contentType = sourceResponse.Content.Headers.ContentType?.ToString()
            ?? ResolveContentType(extension);

        Response.StatusCode = (int)sourceResponse.StatusCode;
        Response.ContentType = contentType;
        Response.Headers.CacheControl = "public,max-age=3600";

        if (sourceResponse.Content.Headers.ContentLength is long contentLength)
            Response.ContentLength = contentLength;

        if (!InlineExtensions.Contains(extension) && !string.IsNullOrWhiteSpace(fileName))
        {
            ContentDispositionHeaderValue contentDisposition = new("attachment")
            {
                FileNameStar = fileName
            };

            Response.Headers.ContentDisposition = contentDisposition.ToString();
        }

        await sourceResponse.Content.CopyToAsync(Response.Body, cancellationToken);

        return new EmptyResult();
    }

    private bool HasObjectStorageConfiguration()
    {
        return !string.IsNullOrWhiteSpace(_options.Endpoint)
            && !string.IsNullOrWhiteSpace(_options.AccessKey)
            && !string.IsNullOrWhiteSpace(_options.SecretKey);
    }

    private bool IsAllowedPublicBucket(string bucketName)
    {
        if (_options.AllowedPublicBuckets.Count == 0)
            return string.Equals(bucketName, _options.CongressDocumentsBucket, StringComparison.OrdinalIgnoreCase)
                || string.Equals(bucketName, _options.CongressImagesBucket, StringComparison.OrdinalIgnoreCase);

        return _options.AllowedPublicBuckets.Contains(bucketName);
    }

    private IActionResult AssetProblemPage(int statusCode, string title, string message)
    {
        Response.Headers.CacheControl = "no-store,no-cache";
        Response.Headers.Pragma = "no-cache";
        Response.Headers.Expires = "0";

        string encodedTitle = WebUtility.HtmlEncode(title);
        string encodedMessage = WebUtility.HtmlEncode(message);

        string html = $$"""
            <!doctype html>
            <html lang="tr">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>{{encodedTitle}}</title>
                <style>
                    body{font-family:Arial,Helvetica,sans-serif;background:#f7f5f0;color:#0b1f3a;margin:0;min-height:100vh;display:flex;align-items:center;justify-content:center;padding:24px;}
                    .box{max-width:560px;background:#fff;border:1px solid #e6e1d8;border-radius:16px;box-shadow:0 18px 40px rgba(15,31,58,.08);padding:32px;text-align:center;}
                    .icon{width:56px;height:56px;margin:0 auto 16px;border-radius:50%;display:flex;align-items:center;justify-content:center;background:#fff1f1;color:#ff5b57;font-size:28px;}
                    h1{font-size:24px;line-height:1.25;margin:0 0 12px;font-weight:700;}
                    p{font-size:16px;line-height:1.6;margin:0 0 24px;color:#526071;}
                    button{border:0;border-radius:10px;background:#ff5b57;color:#fff;padding:12px 18px;font-weight:700;cursor:pointer;}
                </style>
            </head>
            <body>
                <main class="box" role="main">
                    <div class="icon" aria-hidden="true">!</div>
                    <h1>{{encodedTitle}}</h1>
                    <p>{{encodedMessage}}</p>
                    <button type="button" onclick="history.length > 1 ? history.back() : window.close();">Geri Dön</button>
                </main>
            </body>
            </html>
            """;

        return new ContentResult
        {
            StatusCode = statusCode,
            ContentType = "text/html; charset=utf-8",
            Content = html
        };
    }

    private static HttpRequestMessage CreateSignedS3GetRequest(
        string requestUrl,
        string accessKey,
        string secretKey,
        string region)
    {
        Uri uri = new(requestUrl, UriKind.Absolute);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string amzDate = now.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        string dateStamp = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        string hostHeader = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
        string signedHeaders = "host;x-amz-content-sha256;x-amz-date";
        string credentialScope = $"{dateStamp}/{region}/{S3ServiceName}/{Aws4Request}";

        string canonicalHeaders =
            $"host:{hostHeader}\n" +
            $"x-amz-content-sha256:{EmptyPayloadSha256}\n" +
            $"x-amz-date:{amzDate}\n";

        string canonicalRequest = string.Join('\n',
            "GET",
            uri.AbsolutePath,
            string.Empty,
            canonicalHeaders,
            signedHeaders,
            EmptyPayloadSha256);

        string canonicalRequestHash = Sha256Hex(canonicalRequest);

        string stringToSign = string.Join('\n',
            "AWS4-HMAC-SHA256",
            amzDate,
            credentialScope,
            canonicalRequestHash);

        byte[] signingKey = GetSignatureKey(secretKey, dateStamp, region, S3ServiceName);
        string signature = ToHexString(HmacSha256(signingKey, stringToSign));

        string authorizationHeader =
            $"AWS4-HMAC-SHA256 Credential={accessKey}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";

        HttpRequestMessage request = new(HttpMethod.Get, uri);
        request.Headers.Host = hostHeader;
        request.Headers.TryAddWithoutValidation("x-amz-content-sha256", EmptyPayloadSha256);
        request.Headers.TryAddWithoutValidation("x-amz-date", amzDate);
        request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);

        return request;
    }

    private static string BuildObjectStorageObjectUrl(string endpoint, string bucketName, string objectName)
    {
        string encodedBucketName = Uri.EscapeDataString(bucketName.Trim().Trim('/'));
        string encodedObjectName = string.Join(
            '/',
            objectName
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Uri.EscapeDataString));

        return CombineUrl(endpoint, encodedBucketName, encodedObjectName);
    }

    private static string NormalizeObjectStorageEndpoint(string endpoint, bool useSsl)
    {
        string normalizedEndpoint = endpoint.Trim().TrimEnd('/');

        if (normalizedEndpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || normalizedEndpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return normalizedEndpoint;

        string scheme = useSsl ? "https" : "http";

        return $"{scheme}://{normalizedEndpoint}";
    }

    private static string NormalizeObjectName(string value)
    {
        string normalized = value.Trim().Replace('\\', '/').TrimStart('/');

        return Uri.UnescapeDataString(normalized);
    }

    private static bool ContainsUnsafePathSegment(string objectName)
    {
        return objectName
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(segment => segment is "." or "..");
    }

    private static string ResolveContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".zip" => "application/zip",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };
    }

    private static string Sha256Hex(string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        byte[] hash = SHA256.HashData(bytes);

        return ToHexString(hash);
    }

    private static byte[] GetSignatureKey(string key, string dateStamp, string regionName, string serviceName)
    {
        byte[] kDate = HmacSha256(Encoding.UTF8.GetBytes($"AWS4{key}"), dateStamp);
        byte[] kRegion = HmacSha256(kDate, regionName);
        byte[] kService = HmacSha256(kRegion, serviceName);

        return HmacSha256(kService, Aws4Request);
    }

    private static byte[] HmacSha256(byte[] key, string data)
    {
        using HMACSHA256 hmac = new(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    private static string ToHexString(byte[] bytes)
    {
        return Convert.ToHexString(bytes).ToLowerInvariant();
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
