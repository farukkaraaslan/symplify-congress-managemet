using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.Application.Features.AbstractBook.Models;
using Symplify.BackOffice.Application.Features.AbstractBook.Queries.GetPage;
using Symplify.BackOffice.Application.Features.AbstractBook.Queries.GetPdf;
using Symplify.BackOffice.Application.Features.AbstractBook.Queries.GetWord;
using Symplify.BackOffice.WebUI.Models.AbstractBook;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture?}/abstract-book")]
public sealed class AbstractBookController : Controller
{
    private readonly IMediator _mediator;
    private readonly ILogger<AbstractBookController> _logger;

    public AbstractBookController(
        IMediator mediator,
        ILogger<AbstractBookController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(Guid? congressId, CancellationToken cancellationToken)
    {
        string? culture = RouteData.Values["culture"]?.ToString();
        AbstractBookPageResponse page = await _mediator.Send(new GetAbstractBookPageQuery
        {
            CongressId = congressId,
            Culture = culture
        }, cancellationToken);

        return View(new AbstractBookIndexViewModel
        {
            Page = page,
            Export = new AbstractBookExportViewModel
            {
                CongressId = page.SelectedCongressId ?? Guid.Empty
            }
        });
    }

    [HttpPost("pdf")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pdf(
        [Bind(Prefix = "Export")] AbstractBookExportViewModel model,
        [FromForm(Name = "preview")] bool preview,
        [FromForm(Name = "coverImageSelected")] bool coverImageSelected,
        [FromForm(Name = "Export.CoverImageFile")] IFormFile? coverImageFile,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BuildValidationError();

        try
        {
            AbstractBookOptionsDto options = await BuildOptionsAsync(
                model,
                coverImageSelected,
                coverImageFile,
                cancellationToken);
            AbstractBookFileResponse response = await _mediator.Send(new GetAbstractBookPdfQuery
            {
                CongressId = model.CongressId,
                Culture = RouteData.Values["culture"]?.ToString(),
                Filter = model.ToFilter(),
                Options = options
            }, cancellationToken);

            DisableResponseCaching();

            if (preview)
            {
                Response.Headers["Content-Disposition"] = $"inline; filename=\"{response.FileName}\"";
                return File(response.Content, "application/pdf");
            }

            return File(response.Content, "application/pdf", response.FileName);
        }
        catch (InvalidOperationException exception)
        {
            return BuildProblem(
                StatusCodes.Status400BadRequest,
                "Özet kitabı oluşturulamadı.",
                exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Abstract book PDF generation failed for CongressId {CongressId}. TraceId: {TraceId}",
                model.CongressId,
                HttpContext.TraceIdentifier);

            return BuildProblem(
                StatusCodes.Status500InternalServerError,
                "Özet kitabı PDF çıktısı oluşturulamadı.",
                $"Beklenmeyen bir hata oluştu. İzleme kodu: {HttpContext.TraceIdentifier}");
        }
    }

    [HttpPost("word")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Word(
        [Bind(Prefix = "Export")] AbstractBookExportViewModel model,
        [FromForm(Name = "coverImageSelected")] bool coverImageSelected,
        [FromForm(Name = "Export.CoverImageFile")] IFormFile? coverImageFile,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BuildValidationError();

        try
        {
            AbstractBookOptionsDto options = await BuildOptionsAsync(
                model,
                coverImageSelected,
                coverImageFile,
                cancellationToken);
            AbstractBookFileResponse response = await _mediator.Send(new GetAbstractBookWordQuery
            {
                CongressId = model.CongressId,
                Culture = RouteData.Values["culture"]?.ToString(),
                Filter = model.ToFilter(),
                Options = options
            }, cancellationToken);

            DisableResponseCaching();
            return File(
                response.Content,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                response.FileName);
        }
        catch (InvalidOperationException exception)
        {
            return BuildProblem(
                StatusCodes.Status400BadRequest,
                "Özet kitabı oluşturulamadı.",
                exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Abstract book Word generation failed for CongressId {CongressId}. TraceId: {TraceId}",
                model.CongressId,
                HttpContext.TraceIdentifier);

            return BuildProblem(
                StatusCodes.Status500InternalServerError,
                "Özet kitabı Word çıktısı oluşturulamadı.",
                $"Beklenmeyen bir hata oluştu. İzleme kodu: {HttpContext.TraceIdentifier}");
        }
    }

    private async Task<AbstractBookOptionsDto> BuildOptionsAsync(
        AbstractBookExportViewModel model,
        bool coverImageSelected,
        IFormFile? explicitlyBoundCoverImage,
        CancellationToken cancellationToken)
    {
        IFormFile? coverImage = explicitlyBoundCoverImage is { Length: > 0 }
            ? explicitlyBoundCoverImage
            : model.CoverImageFile is { Length: > 0 }
                ? model.CoverImageFile
                : null;

        // IFormFile nested model binding can be inconsistent when the action uses
        // Bind(Prefix = "Export"). Therefore, always fall back to the raw multipart
        // collection and resolve the exact HTML field name directly.
        if (coverImage is null && Request.HasFormContentType)
        {
            IFormCollection form = await Request.ReadFormAsync(cancellationToken);
            coverImage = form.Files.GetFile("Export.CoverImageFile")
                ?? form.Files.GetFile(nameof(AbstractBookExportViewModel.CoverImageFile))
                ?? form.Files.FirstOrDefault(file =>
                    file.Name.EndsWith(
                        $".{nameof(AbstractBookExportViewModel.CoverImageFile)}",
                        StringComparison.OrdinalIgnoreCase));
        }

        if (coverImageSelected && coverImage is null)
        {
            throw new InvalidOperationException(
                "Kapak dosyası seçilmiş görünüyor ancak sunucuya ulaşmadı. Sayfayı yenileyip dosyayı tekrar seçin.");
        }

        (byte[]? coverBytes, string? coverContentType) = await ReadImageFileAsync(
            coverImage,
            8 * 1024 * 1024,
            cancellationToken);

        if (coverBytes is { Length: > 0 })
        {
            _logger.LogInformation(
                "Abstract book cover received. FileName: {FileName}, Length: {Length}, ContentType: {ContentType}, TraceId: {TraceId}",
                coverImage?.FileName,
                coverBytes.Length,
                coverContentType,
                HttpContext.TraceIdentifier);
        }

        return model.ToOptions(
            coverBytes,
            coverContentType);
    }

    private void DisableResponseCaching()
    {
        Response.Headers["Cache-Control"] = "no-store, no-cache, max-age=0";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";
    }

    private static async Task<(byte[]? Content, string? ContentType)> ReadImageFileAsync(
        IFormFile? file,
        int maxBytes,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0)
            return (null, null);

        if (file.Length > maxBytes)
            throw new InvalidOperationException("Yüklenen görsel boyutu izin verilen sınırı aşıyor.");

        await using MemoryStream stream = new();
        await file.CopyToAsync(stream, cancellationToken);
        byte[] bytes = stream.ToArray();
        string? detectedContentType = DetectSupportedImageContentType(bytes);

        if (detectedContentType is null)
        {
            throw new InvalidOperationException(
                "Kapak görselinin gerçek dosya içeriği desteklenmiyor. Yalnızca geçerli bir PNG veya JPG dosyası yükleyin.");
        }

        return (bytes, detectedContentType);
    }

    private static string? DetectSupportedImageContentType(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 8
            && bytes[0] == 0x89
            && bytes[1] == 0x50
            && bytes[2] == 0x4E
            && bytes[3] == 0x47
            && bytes[4] == 0x0D
            && bytes[5] == 0x0A
            && bytes[6] == 0x1A
            && bytes[7] == 0x0A)
        {
            return "image/png";
        }

        if (bytes.Length >= 3
            && bytes[0] == 0xFF
            && bytes[1] == 0xD8
            && bytes[2] == 0xFF)
        {
            return "image/jpeg";
        }

        return null;
    }

    private IActionResult BuildValidationError()
    {
        string message = string.Join(" ", ModelState.Values
            .SelectMany(value => value.Errors)
            .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                ? error.Exception?.Message
                : error.ErrorMessage)
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Distinct(StringComparer.Ordinal));

        return BuildProblem(
            StatusCodes.Status400BadRequest,
            "Özet kitabı ayarları geçerli değil.",
            string.IsNullOrWhiteSpace(message)
                ? "Form alanlarını kontrol edip yeniden deneyin."
                : message);
    }

    private ObjectResult BuildProblem(int statusCode, string title, string detail)
    {
        return StatusCode(statusCode, new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = HttpContext.Request.Path
        });
    }
}
