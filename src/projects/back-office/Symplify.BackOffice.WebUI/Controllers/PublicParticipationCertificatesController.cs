using Core.Application.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Symplify.BackOffice.Application.Features.ParticipationCertificates.Services;
using Symplify.BackOffice.WebUI.Models.ParticipationCertificates;

namespace Symplify.BackOffice.WebUI.Controllers;

[AllowAnonymous]
[EnableRateLimiting("public-certificate")]
[Route("public/certificates")]
public sealed class PublicParticipationCertificatesController : Controller
{
    private readonly IParticipationCertificateService _service;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<PublicParticipationCertificatesController> _logger;

    public PublicParticipationCertificatesController(
        IParticipationCertificateService service,
        IObjectStorageService objectStorageService,
        ILogger<PublicParticipationCertificatesController> logger)
    {
        _service = service;
        _objectStorageService = objectStorageService;
        _logger = logger;
    }

    [HttpGet("verify/{verificationCode}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Verify(string verificationCode, CancellationToken cancellationToken)
    {
        return await RenderVerificationAsync(
            await _service.GetVerificationAsync(verificationCode, cancellationToken));
    }

    // Geriye dönük uyumluluk: daha önce üretilen GUID QR bağlantıları çalışmaya devam eder.
    [HttpGet("verify/{publicId:guid}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> VerifyLegacy(Guid publicId, CancellationToken cancellationToken)
    {
        return await RenderVerificationAsync(
            await _service.GetVerificationAsync(publicId, cancellationToken));
    }

    private IActionResult RenderVerification(ParticipationCertificateVerificationDto? verification)
    {
        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive, nosnippet";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        if (verification is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("Verify", new PublicParticipationCertificateViewModel
            {
                StatusCode = StatusCodes.Status404NotFound,
                Title = "Belge Bulunamadı",
                Message = "Bu doğrulama koduna ait katılım belgesi bulunamadı.",
                IsVerification = true,
                IsValid = false
            });
        }

        Response.StatusCode = verification.IsValid ? StatusCodes.Status200OK : StatusCodes.Status410Gone;
        return View("Verify", new PublicParticipationCertificateViewModel
        {
            StatusCode = Response.StatusCode,
            Title = verification.IsValid ? "Belge Doğrulandı" : "Belge Geçerli Değil",
            Message = verification.Message ?? string.Empty,
            IsVerification = true,
            IsValid = verification.IsValid,
            PublicId = verification.PublicId,
            VerificationCode = verification.VerificationCode,
            CongressName = verification.CongressName,
            SubmissionNumber = verification.SubmissionNumber,
            SubmissionTitle = verification.SubmissionTitle,
            ParticipantName = verification.ParticipantName,
            Culture = verification.Culture,
            GeneratedAt = verification.GeneratedAt
        });
    }

    private Task<IActionResult> RenderVerificationAsync(ParticipationCertificateVerificationDto? verification)
        => Task.FromResult(RenderVerification(verification));

    [HttpGet("{publicId:guid}/{token}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Open(
        Guid publicId,
        string token,
        CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        Response.Headers.Pragma = "no-cache";
        Response.Headers.Expires = "0";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive, nosnippet";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        ParticipationCertificatePublicAccessResult access = await _service.ResolvePublicAccessAsync(
            publicId,
            token,
            cancellationToken);

        switch (access.Status)
        {
            case ParticipationCertificatePublicAccessStatus.Available when access.File is not null:
                return await StreamFileAsync(access.File, cancellationToken);

            case ParticipationCertificatePublicAccessStatus.Revoked:
                return RenderUnavailable(
                    StatusCodes.Status410Gone,
                    "Belge Artık Geçerli Değil",
                    access.Message ?? "Bu katılım belgesi kongre yönetimi tarafından kaldırılmıştır.");

            case ParticipationCertificatePublicAccessStatus.NotPublished:
                return RenderUnavailable(
                    StatusCodes.Status404NotFound,
                    "Belge Bulunamadı",
                    "Bu belge henüz yayınlanmamış veya erişime kapatılmıştır.");

            default:
                return RenderUnavailable(
                    StatusCodes.Status404NotFound,
                    "Belge Bulunamadı",
                    "Bağlantı geçersiz, eksik veya süresi dolmuş olabilir.");
        }
    }

    private async Task<IActionResult> StreamFileAsync(
        ParticipationCertificateStoredFileDto file,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(file.BucketName) || string.IsNullOrWhiteSpace(file.ObjectName))
        {
            return RenderUnavailable(
                StatusCodes.Status404NotFound,
                "Belge Bulunamadı",
                "Belge dosyasına ulaşılamadı.");
        }

        try
        {
            Stream stream = await _objectStorageService.OpenReadAsync(
                file.BucketName,
                file.ObjectName,
                cancellationToken);

            string fileName = string.IsNullOrWhiteSpace(file.FileName)
                ? $"participation-certificate-{file.Id:N}.pdf"
                : file.FileName;

            Response.Headers.ContentDisposition =
                $"inline; filename=\"{SanitizeHeaderFileName(fileName)}\"";

            return File(
                stream,
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/pdf" : file.ContentType,
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
                "Public participation certificate file read failed. CertificateId: {CertificateId}",
                file.Id);

            return RenderUnavailable(
                StatusCodes.Status503ServiceUnavailable,
                "Belge Geçici Olarak Açılamıyor",
                "Belge dosyasına şu anda ulaşılamıyor. Lütfen daha sonra tekrar deneyiniz.");
        }
    }

    private IActionResult RenderUnavailable(int statusCode, string title, string message)
    {
        Response.StatusCode = statusCode;
        return View("Unavailable", new PublicParticipationCertificateViewModel
        {
            StatusCode = statusCode,
            Title = title,
            Message = message
        });
    }

    private static string SanitizeHeaderFileName(string value)
        => value.Replace("\"", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
}
