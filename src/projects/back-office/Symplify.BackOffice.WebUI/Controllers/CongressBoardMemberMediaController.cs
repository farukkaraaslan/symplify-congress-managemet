using Core.Application.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture=tr-TR}/congress-board-member-media")]
public sealed class CongressBoardMemberMediaController : Controller
{
    private readonly ICongressBoardMemberRepository _memberRepository;
    private readonly ICongressBoardRepository _boardRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ObjectStorageOptions _storageOptions;
    private readonly ILogger<CongressBoardMemberMediaController> _logger;

    public CongressBoardMemberMediaController(
        ICongressBoardMemberRepository memberRepository,
        ICongressBoardRepository boardRepository,
        IObjectStorageService objectStorageService,
        IOptions<ObjectStorageOptions> storageOptions,
        ILogger<CongressBoardMemberMediaController> logger)
    {
        _memberRepository = memberRepository;
        _boardRepository = boardRepository;
        _objectStorageService = objectStorageService;
        _storageOptions = storageOptions.Value;
        _logger = logger;
    }

    [HttpGet("{congressId:guid}/{id:guid}/photo")]
    public async Task<IActionResult> Photo(
        Guid congressId,
        Guid id,
        CancellationToken cancellationToken)
    {
        CongressBoardMember? member =
            await LoadMemberForCongressAsync(congressId, id, cancellationToken);

        if (member is null)
            return NotFound();

        return await CreateMediaResultAsync(
            member.ImageBucketName,
            member.ImageObjectName,
            member.ImagePath,
            member.ImageContentType,
            "image/jpeg",
            cancellationToken);
    }

    [HttpGet("{congressId:guid}/{id:guid}/signature")]
    public async Task<IActionResult> Signature(
        Guid congressId,
        Guid id,
        CancellationToken cancellationToken)
    {
        CongressBoardMember? member =
            await LoadMemberForCongressAsync(congressId, id, cancellationToken);

        if (member is null)
            return NotFound();

        return await CreateMediaResultAsync(
            member.SignatureBucketName,
            member.SignatureObjectName,
            member.SignaturePath,
            member.SignatureContentType,
            "image/png",
            cancellationToken);
    }

    private async Task<CongressBoardMember?> LoadMemberForCongressAsync(
        Guid congressId,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (congressId == Guid.Empty || id == Guid.Empty)
            return null;

        CongressBoardMember? member = await _memberRepository
            .Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == id && item.DeletedDate == null,
                cancellationToken);

        if (member is null)
            return null;

        bool belongsToCongress = await _boardRepository
            .Query()
            .AsNoTracking()
            .AnyAsync(
                board =>
                    board.Id == member.CongressBoardId &&
                    board.CongressId == congressId &&
                    board.DeletedDate == null,
                cancellationToken);

        return belongsToCongress ? member : null;
    }

    private async Task<IActionResult> CreateMediaResultAsync(
        string? storedBucketName,
        string? storedObjectName,
        string? legacyPath,
        string? storedContentType,
        string defaultContentType,
        CancellationToken cancellationToken)
    {
        MediaLocation? location = ResolveObjectStorageLocation(
            storedBucketName,
            storedObjectName,
            legacyPath);

        if (location is not null)
        {
            try
            {
                ObjectStorageFileInfo? info =
                    await _objectStorageService.GetFileInfoAsync(
                        location.BucketName,
                        location.ObjectName,
                        cancellationToken);

                if (info is null)
                    return NotFound();

                Stream stream = await _objectStorageService.OpenReadAsync(
                    location.BucketName,
                    location.ObjectName,
                    cancellationToken);

                string contentType = ResolveImageContentType(
                    storedContentType,
                    info.ContentType,
                    location.ObjectName,
                    defaultContentType);

                // Yönetim ekranında fotoğraf değiştiğinde aynı URL kullanılıyor.
                // Cache kapalı tutulur; yeni upload table reload sonrası hemen görünür.
                Response.Headers.CacheControl = "private,no-store,no-cache,max-age=0,must-revalidate";
                Response.Headers.Pragma = "no-cache";
                Response.Headers.Expires = "0";
                Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";

                return File(stream, contentType, enableRangeProcessing: true);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Kurul üyesi media dosyası object storage üzerinden okunamadı. Bucket: {Bucket}, Object: {Object}",
                    location.BucketName,
                    location.ObjectName);

                return NotFound();
            }
        }

        string? normalizedLegacyPath = Normalize(legacyPath);

        // Gerçek external/legacy URL ise davranışı koru. Internal MinIO URL'leri
        // ResolveObjectStorageLocation tarafından yukarıda yakalanır.
        if (Uri.TryCreate(normalizedLegacyPath, UriKind.Absolute, out Uri? externalUri) &&
            (externalUri.Scheme == Uri.UriSchemeHttp ||
             externalUri.Scheme == Uri.UriSchemeHttps))
        {
            return Redirect(externalUri.ToString());
        }

        if (!string.IsNullOrWhiteSpace(normalizedLegacyPath))
        {
            if (normalizedLegacyPath.StartsWith("~/", StringComparison.Ordinal))
                return Redirect(normalizedLegacyPath[1..]);

            if (normalizedLegacyPath.StartsWith("/", StringComparison.Ordinal))
                return Redirect(normalizedLegacyPath);
        }

        return NotFound();
    }

    private MediaLocation? ResolveObjectStorageLocation(
        string? storedBucketName,
        string? storedObjectName,
        string? legacyPath)
    {
        string? bucketName = FirstNonEmpty(
            storedBucketName,
            _storageOptions.Buckets.CongressImages);

        string? objectName = Normalize(storedObjectName);

        // Yeni storage modelinde authoritative alan ImageObjectName/SignatureObjectName.
        if (!string.IsNullOrWhiteSpace(bucketName) &&
            !string.IsNullOrWhiteSpace(objectName))
        {
            return new MediaLocation(
                bucketName.Trim(),
                NormalizeObjectName(bucketName, objectName));
        }

        string? path = Normalize(legacyPath);

        if (string.IsNullOrWhiteSpace(path))
            return null;

        // Eski DB kayıtlarında internal MinIO presigned URL saklanmış olabilir:
        // https://minio:9000/symplify-congress-images/backoffice/...?... 
        if (Uri.TryCreate(path, UriKind.Absolute, out Uri? uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            string[] segments = uri.AbsolutePath
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length >= 2)
            {
                string urlBucket = Uri.UnescapeDataString(segments[0]);
                string urlObject = string.Join(
                    "/",
                    segments.Skip(1).Select(Uri.UnescapeDataString));

                bool looksLikeInternalMinio =
                    uri.Host.Equals("minio", StringComparison.OrdinalIgnoreCase) ||
                    uri.Host.Contains("minio", StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(bucketName) &&
                     urlBucket.Equals(bucketName, StringComparison.OrdinalIgnoreCase));

                if (looksLikeInternalMinio &&
                    !string.IsNullOrWhiteSpace(urlObject))
                {
                    return new MediaLocation(urlBucket, urlObject);
                }
            }

            return null;
        }

        if (string.IsNullOrWhiteSpace(bucketName))
            return null;

        if (path.StartsWith("/", StringComparison.Ordinal) ||
            path.StartsWith("~/", StringComparison.Ordinal))
        {
            return null;
        }

        return new MediaLocation(
            bucketName.Trim(),
            NormalizeObjectName(bucketName, path));
    }

    private static string NormalizeObjectName(
        string bucketName,
        string objectName)
    {
        string normalized = objectName
            .Trim()
            .TrimStart('/')
            .Replace('\\', '/');

        string prefix = bucketName.Trim().Trim('/') + "/";

        if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            normalized = normalized[prefix.Length..];

        return normalized;
    }

    private static string ResolveImageContentType(
        string? storedContentType,
        string? storageContentType,
        string objectName,
        string fallback)
    {
        foreach (string? candidate in new[] { storedContentType, storageContentType })
        {
            string? normalized = Normalize(candidate)?.ToLowerInvariant();

            if (normalized is "image/jpeg" or "image/jpg")
                return "image/jpeg";

            if (normalized == "image/png")
                return "image/png";

            if (normalized == "image/webp")
                return "image/webp";
        }

        return Path.GetExtension(objectName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => fallback
        };
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record MediaLocation(
        string BucketName,
        string ObjectName);
}
