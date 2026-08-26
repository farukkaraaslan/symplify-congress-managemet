using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.Application.Features.ContentAssets.Commands;
using Symplify.BackOffice.Application.Features.ContentAssets.Commands.Upload;
using Symplify.BackOffice.Application.Services.Urls;
using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.WebUI.Models.ContentAssets;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture=tr-TR}/[controller]/[action]")]
public sealed class ContentAssetsController : Controller
{
    private readonly IMediator _mediator;
    private readonly IBackOfficeViewLocalizer _localizer;
    private readonly IPublicUrlService _publicUrlService;

    public ContentAssetsController(
        IMediator mediator,
        IBackOfficeViewLocalizer localizer,
        IPublicUrlService publicUrlService)
    {
        _mediator = mediator;
        _localizer = localizer;
        _publicUrlService = publicUrlService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(
        [FromForm] UploadContentAssetViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.File is null || model.File.Length <= 0)
        {
            return BadRequest(new
            {
                success = false,
                message = GetText(
                    "BackOffice.ContentAssets.Validation.FileRequired",
                    "Dosya seçimi zorunludur.")
            });
        }

        try
        {
            using Stream fileStream = model.File.OpenReadStream();

            UploadedContentAssetResponse response = await _mediator.Send(
                new UploadContentAssetCommand
                {
                    CongressId = model.CongressId,
                    File = new ContentAssetFileInputDto
                    {
                        OriginalFileName = model.File.FileName,
                        ContentType = model.File.ContentType,
                        Length = model.File.Length,
                        Content = fileStream
                    }
                },
                cancellationToken);

            string assetUrl = BuildPublicAssetUrl(response.BucketName, response.ObjectName);

            return Json(new
            {
                success = true,
                url = assetUrl,
                fileName = response.OriginalFileName,
                contentType = response.ContentType,
                fileExtension = response.FileExtension,
                fileSize = response.FileSize,
                fileSizeText = FormatFileSize(response.FileSize),
                message = GetText(
                    "BackOffice.ContentAssets.Messages.Uploaded",
                    "Dosya yüklendi ve bağlantı hazırlandı.")
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new
            {
                success = false,
                message = GetExceptionMessage(exception)
            });
        }
    }

    private string BuildPublicAssetUrl(string bucketName, string objectName)
    {
        string encodedBucketName = Uri.EscapeDataString(bucketName.Trim());
        string encodedObjectName = string.Join(
            '/',
            objectName
                .Trim()
                .TrimStart('/')
                .Replace('\\', '/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Uri.EscapeDataString));

        return _publicUrlService.Build($"/public-assets/{encodedBucketName}/{encodedObjectName}");
    }

    private string GetExceptionMessage(Exception exception)
    {
        if (IsObjectStorageAccessKeyException(exception))
        {
            return GetText(
                "BackOffice.ContentAssets.Messages.StorageConfigurationInvalid",
                "Dosya yükleme servisi yapılandırması hatalı. MinIO erişim bilgilerini kontrol edin.");
        }

        return !string.IsNullOrWhiteSpace(exception.Message)
            ? GetText(exception.Message, exception.Message)
            : GetText("Common.GenericError", "Beklenmeyen bir işlem hatası oluştu.");
    }

    private static bool IsObjectStorageAccessKeyException(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current.Message.Contains("Access Key Id", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("InvalidAccessKeyId", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private string GetText(string key, string fallback)
    {
        string value = _localizer.GetStringValue(key);

        if (string.IsNullOrWhiteSpace(value) || string.Equals(value, key, StringComparison.OrdinalIgnoreCase))
            return fallback;

        return value;
    }

    private static string FormatFileSize(long size)
    {
        if (size <= 0)
            return "-";

        double bytes = size;
        string[] units = { "B", "KB", "MB", "GB" };
        int unitIndex = 0;

        while (bytes >= 1024 && unitIndex < units.Length - 1)
        {
            bytes /= 1024;
            unitIndex++;
        }

        return $"{bytes:0.##} {units[unitIndex]}";
    }
}
