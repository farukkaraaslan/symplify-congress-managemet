using Core.Application.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Application.Common.Storage;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.WebUI.Controllers;

/// <summary>
/// Kabul mektubunu private object storage'dan public, tahmin edilmesi zor bir
/// URL üzerinden server-side stream eder.
///
/// URL hem random entity Id hem de mevcut verification code içerir.
/// MinIO endpoint'i browser'a hiçbir zaman verilmez.
/// </summary>
[AllowAnonymous]
[EnableRateLimiting("public-certificate")]
[Route("public/acceptance-letters")]
public sealed class PublicAcceptanceLettersController : Controller
{
    private readonly ISubmissionAcceptanceLetterRepository _acceptanceLetterRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ObjectStorageOptions _storageOptions;
    private readonly ILogger<PublicAcceptanceLettersController> _logger;

    public PublicAcceptanceLettersController(
        ISubmissionAcceptanceLetterRepository acceptanceLetterRepository,
        IObjectStorageService objectStorageService,
        IOptions<ObjectStorageOptions> storageOptions,
        ILogger<PublicAcceptanceLettersController> logger)
    {
        _acceptanceLetterRepository = acceptanceLetterRepository;
        _objectStorageService = objectStorageService;
        _storageOptions = storageOptions.Value;
        _logger = logger;
    }

    [HttpGet("{id:guid}/{code}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Open(
        Guid id,
        string code,
        CancellationToken cancellationToken)
    {
        ApplySecurityHeaders();

        if (id == Guid.Empty || string.IsNullOrWhiteSpace(code))
            return NotFound();

        string normalizedCode = code.Trim().ToUpperInvariant();

        SubmissionAcceptanceLetter? letter = await _acceptanceLetterRepository
            .Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item =>
                    item.Id == id &&
                    item.LetterNumber == normalizedCode &&
                    item.DeletedDate == null,
                cancellationToken);

        if (letter is null)
            return NotFound();

        string? objectName = FirstNonEmpty(
            letter.PdfObjectName,
            letter.PdfFilePath);

        if (string.IsNullOrWhiteSpace(objectName))
            return NotFound();

        string? bucketName = FirstNonEmpty(
            letter.PdfBucketName,
            _storageOptions.Buckets.Submissions);

        if (string.IsNullOrWhiteSpace(bucketName))
            return NotFound();

        try
        {
            ObjectStorageFileInfo? info =
                await _objectStorageService.GetFileInfoAsync(
                    bucketName,
                    objectName,
                    cancellationToken);

            if (info is null)
                return NotFound();

            Stream stream = await _objectStorageService.OpenReadAsync(
                bucketName,
                objectName,
                cancellationToken);

            string contentType = FirstNonEmpty(
                letter.PdfContentType,
                info.ContentType,
                "application/pdf")!;

            string fileName = string.IsNullOrWhiteSpace(letter.FileName)
                ? $"acceptance-letter-{letter.Id:N}.pdf"
                : letter.FileName.Trim();

            Response.Headers[HeaderNames.ContentDisposition] =
                $"inline; filename*=UTF-8''{Uri.EscapeDataString(fileName)}";

            return File(
                stream,
                contentType,
                enableRangeProcessing: true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Public acceptance letter read failed. AcceptanceLetterId: {AcceptanceLetterId}",
                letter.Id);

            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    private void ApplySecurityHeaders()
    {
        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        Response.Headers.Pragma = "no-cache";
        Response.Headers.Expires = "0";
        Response.Headers["X-Robots-Tag"] =
            "noindex, nofollow, noarchive, nosnippet";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
}
