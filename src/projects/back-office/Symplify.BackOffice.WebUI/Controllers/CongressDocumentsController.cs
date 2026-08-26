using Core.Application.Requests;
using Core.Application.Storage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressDocuments.Commands;
using Symplify.BackOffice.Application.Features.CongressDocuments.Commands.Create;
using Symplify.BackOffice.Application.Features.CongressDocuments.Commands.Delete;
using Symplify.BackOffice.Application.Features.CongressDocuments.Commands.Reorder;
using Symplify.BackOffice.Application.Features.CongressDocuments.Commands.Update;
using Symplify.BackOffice.Application.Features.CongressDocuments.Queries.GetById;
using Symplify.BackOffice.Application.Features.CongressDocuments.Queries.GetForUpdate;
using Symplify.BackOffice.Application.Features.CongressDocuments.Queries.GetList;
using Symplify.BackOffice.Application.Features.DocumentTypes.Queries.GetList;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.WebUI.Models.CongressDocuments;
using Symplify.BackOffice.WebUI.Models.Shared.DataTables;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture=tr-TR}/[controller]/[action]")]
public sealed class CongressDocumentsController : Controller
{
    private const long MaxFileSizeBytes = 50 * 1024 * 1024;
    private const long MaxCoverImageSizeBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".jpg", ".jpeg", ".png", ".webp"
    };

    private static readonly HashSet<string> AllowedCoverImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private readonly IMediator _mediator;
    private readonly IObjectStorageService _objectStorageService;
    private readonly IApplicationLanguageProvider _applicationLanguageProvider;
    private readonly IBackOfficeViewLocalizer _localizer;

    public CongressDocumentsController(
        IMediator mediator,
        IObjectStorageService objectStorageService,
        IApplicationLanguageProvider applicationLanguageProvider,
        IBackOfficeViewLocalizer localizer)
    {
        _mediator = mediator;
        _objectStorageService = objectStorageService;
        _applicationLanguageProvider = applicationLanguageProvider;
        _localizer = localizer;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GetList(
        [FromForm] DataTableRequest request,
        [FromForm] Guid congressId,
        CancellationToken cancellationToken)
    {
        if (congressId == Guid.Empty)
        {
            return Json(new
            {
                draw = request.Draw,
                recordsTotal = 0,
                recordsFiltered = 0,
                data = Array.Empty<object>()
            });
        }

        DataTableQueryOptions tableOptions = DataTableQueryOptions.From(
            request,
            defaultSortColumn: "order",
            defaultSortDirection: "asc",
            allowedSortColumns: new[]
            {
                "order", "originalFileName", "documentTypeName", "fileSize", "isActive"
            });

        string culture = await ResolveCurrentCultureAsync(cancellationToken);

        var response = await _mediator.Send(
            new GetListCongressDocumentQuery
            {
                CongressId = congressId,
                Culture = culture,
                SearchText = tableOptions.SearchText,
                SortColumn = tableOptions.SortColumn,
                SortDirection = tableOptions.SortDirection,
                PageRequest = new PageRequest
                {
                    Page = tableOptions.Page,
                    PageSize = tableOptions.PageSize
                }
            },
            cancellationToken);

        return Json(new
        {
            draw = request.Draw,
            recordsTotal = response.Count,
            recordsFiltered = response.Count,
            data = response.Items.Select((item, index) => new
            {
                rowNumber = tableOptions.Start + index + 1,
                id = item.Id,
                congressId = item.CongressId,
                documentTypeName = item.DocumentTypeName,
                description = item.Description,
                originalFileName = item.OriginalFileName,
                contentType = item.ContentType,
                fileExtension = item.FileExtension,
                fileSize = item.FileSize,
                fileSizeText = FormatFileSize(item.FileSize),
                order = item.Order,
                isActive = item.IsActive,
                isFallback = item.IsFallback,
                hasCoverImage = !string.IsNullOrWhiteSpace(item.CoverImageBucketName) && !string.IsNullOrWhiteSpace(item.CoverImageObjectName),
                coverImageFileName = item.CoverImageFileName,
                coverImageContentType = item.CoverImageContentType,
                coverImageUrl = BuildPublicAssetUrl(item.CoverImageBucketName, item.CoverImageObjectName),
                downloadUrl = Url.Action("Download", "CongressDocuments", new
                {
                    culture,
                    id = item.Id,
                    congressId = item.CongressId
                })
            })
        });
    }

    [HttpGet]
    public async Task<IActionResult> CreateModal(Guid congressId, CancellationToken cancellationToken)
    {
        CreateCongressDocumentViewModel model = await BuildCreateViewModelAsync(congressId, cancellationToken);
        return PartialView("~/Views/CongressDocuments/_CreateDocumentModal.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromForm] CreateCongressDocumentViewModel model,
        CancellationToken cancellationToken)
    {
        ValidateCreateModel(model);

        if (!ModelState.IsValid)
            return BadRequest(CreateValidationErrorResponse());

        try
        {
            using Stream fileStream = model.File!.OpenReadStream();
            using Stream? coverImageStream = model.CoverImage is not null && model.CoverImage.Length > 0
                ? model.CoverImage.OpenReadStream()
                : null;

            CongressDocumentFileInputDto? coverImageInput = model.CoverImage is not null &&
                model.CoverImage.Length > 0 &&
                coverImageStream is not null
                    ? new CongressDocumentFileInputDto
                    {
                        OriginalFileName = model.CoverImage.FileName,
                        ContentType = model.CoverImage.ContentType,
                        Length = model.CoverImage.Length,
                        Content = coverImageStream
                    }
                    : null;

            CreatedCongressDocumentResponse response = await _mediator.Send(
                new CreateCongressDocumentCommand
                {
                    CongressId = model.CongressId,
                    DocumentTypeId = model.DocumentTypeId,
                    Translations = BuildTranslationInputs(model.Translations),
                    IsActive = model.IsActive,
                    File = new CongressDocumentFileInputDto
                    {
                        OriginalFileName = model.File.FileName,
                        ContentType = model.File.ContentType,
                        Length = model.File.Length,
                        Content = fileStream
                    },
                    CoverImage = coverImageInput
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                id = response.Id,
                message = GetText("BackOffice.CongressDocuments.Messages.Created", "Doküman başarıyla oluşturuldu.")
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

    [HttpGet]
    public async Task<IActionResult> EditModal(Guid id, Guid congressId, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(
            new GetCongressDocumentForUpdateQuery
            {
                Id = id,
                CongressId = congressId
            },
            cancellationToken);

        UpdateCongressDocumentViewModel model = await BuildUpdateViewModelAsync(response, cancellationToken);

        return PartialView("~/Views/CongressDocuments/_UpdateDocumentModal.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        [FromForm] UpdateCongressDocumentViewModel model,
        CancellationToken cancellationToken)
    {
        ValidateUpdateModel(model);

        if (!ModelState.IsValid)
            return BadRequest(CreateValidationErrorResponse());

        try
        {
            CongressDocumentFileInputDto? fileInput = null;
            CongressDocumentFileInputDto? coverImageInput = null;

            using Stream? fileStream = model.File is not null && model.File.Length > 0
                ? model.File.OpenReadStream()
                : null;
            using Stream? coverImageStream = model.CoverImage is not null && model.CoverImage.Length > 0
                ? model.CoverImage.OpenReadStream()
                : null;

            if (model.File is not null && model.File.Length > 0 && fileStream is not null)
            {
                fileInput = new CongressDocumentFileInputDto
                {
                    OriginalFileName = model.File.FileName,
                    ContentType = model.File.ContentType,
                    Length = model.File.Length,
                    Content = fileStream
                };
            }

            if (model.CoverImage is not null && model.CoverImage.Length > 0 && coverImageStream is not null)
            {
                coverImageInput = new CongressDocumentFileInputDto
                {
                    OriginalFileName = model.CoverImage.FileName,
                    ContentType = model.CoverImage.ContentType,
                    Length = model.CoverImage.Length,
                    Content = coverImageStream
                };
            }

            await _mediator.Send(
                new UpdateCongressDocumentCommand
                {
                    Id = model.Id,
                    CongressId = model.CongressId,
                    DocumentTypeId = model.DocumentTypeId,
                    Translations = BuildTranslationInputs(model.Translations),
                    IsActive = model.IsActive,
                    File = fileInput,
                    CoverImage = coverImageInput,
                    RemoveCoverImage = model.RemoveCoverImage
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("BackOffice.CongressDocuments.Messages.Updated", "Doküman başarıyla güncellendi.")
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([FromForm] Guid id, [FromForm] Guid congressId, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty || congressId == Guid.Empty)
        {
            return BadRequest(new
            {
                success = false,
                message = GetText("Common.InvalidRequest", "Geçersiz istek.")
            });
        }

        try
        {
            try
            {
                await _mediator.Send(
                    new GetCongressDocumentForUpdateQuery
                    {
                        Id = id,
                        CongressId = congressId
                    },
                    cancellationToken);
            }
            catch
            {
                // Delete command will return the authoritative business error.
            }

            await _mediator.Send(new DeleteCongressDocumentCommand { Id = id }, cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("BackOffice.CongressDocuments.Messages.Deleted", "Doküman başarıyla silindi.")
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder(
        [FromBody] DataTableReorderRequest request,
        [FromQuery] Guid congressId,
        CancellationToken cancellationToken)
    {
        if (congressId == Guid.Empty || request is null || request.Items.Count == 0)
        {
            return BadRequest(new
            {
                success = false,
                message = GetText("Common.InvalidRequest", "Geçersiz istek.")
            });
        }

        try
        {
            await _mediator.Send(
                new ReorderCongressDocumentCommand
                {
                    CongressId = congressId,
                    Items = request.Items
                        .Where(item => item.Id != Guid.Empty)
                        .Select(item => new ReorderCongressDocumentItemDto
                        {
                            Id = item.Id,
                            Order = item.Order
                        })
                        .ToList()
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("BackOffice.CongressDocuments.Messages.Reordered", "Doküman sıralaması güncellendi.")
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

    [HttpGet]
    public async Task<IActionResult> Download(Guid id, Guid congressId, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty || congressId == Guid.Empty)
            return BadRequest(GetText("Common.InvalidRequest", "Geçersiz istek."));

        GetByIdCongressDocumentResponse document = await _mediator.Send(
            new GetByIdCongressDocumentQuery { Id = id },
            cancellationToken);

        if (document.CongressId != congressId)
            return NotFound();

        if (string.IsNullOrWhiteSpace(document.BucketName) || string.IsNullOrWhiteSpace(document.ObjectName))
            return BadRequest(GetText("BackOffice.CongressDocuments.Validation.ObjectStorageObjectMissing", "Doküman depolama nesne bilgisi bulunamadı."));

        ObjectStorageFileInfo? fileInfo = await _objectStorageService.GetFileInfoAsync(
            document.BucketName,
            document.ObjectName,
            cancellationToken);

        if (fileInfo is null)
            return NotFound();

        string contentType = ResolveDownloadContentType(document.ContentType ?? fileInfo.ContentType, document.ObjectName, document.OriginalFileName);
        string downloadFileName = ResolveDownloadFileName(document);

        Stream stream = await _objectStorageService.OpenReadAsync(
            document.BucketName,
            document.ObjectName,
            cancellationToken);

        Response.Headers["X-Content-Type-Options"] = "nosniff";

        FileStreamResult result = File(stream, contentType, downloadFileName);
        result.EnableRangeProcessing = true;

        return result;
    }

    [HttpGet]
    public async Task<IActionResult> CoverImage(Guid id, Guid congressId, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty || congressId == Guid.Empty)
            return BadRequest(GetText("Common.InvalidRequest", "Geçersiz istek."));

        GetByIdCongressDocumentResponse document = await _mediator.Send(
            new GetByIdCongressDocumentQuery { Id = id },
            cancellationToken);

        if (document.CongressId != congressId)
            return NotFound();

        if (string.IsNullOrWhiteSpace(document.CoverImageBucketName) ||
            string.IsNullOrWhiteSpace(document.CoverImageObjectName))
        {
            return NotFound();
        }

        ObjectStorageFileInfo? fileInfo = await _objectStorageService.GetFileInfoAsync(
            document.CoverImageBucketName,
            document.CoverImageObjectName,
            cancellationToken);

        if (fileInfo is null)
            return NotFound();

        string contentType = ResolveDownloadContentType(
            document.CoverImageContentType ?? fileInfo.ContentType,
            document.CoverImageObjectName,
            document.CoverImageFileName);

        Stream stream = await _objectStorageService.OpenReadAsync(
            document.CoverImageBucketName,
            document.CoverImageObjectName,
            cancellationToken);

        Response.Headers.CacheControl = "public,max-age=86400";
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        FileStreamResult result = File(stream, contentType);
        result.EnableRangeProcessing = true;

        return result;
    }

    private async Task<CreateCongressDocumentViewModel> BuildCreateViewModelAsync(Guid congressId, CancellationToken cancellationToken)
    {
        return new CreateCongressDocumentViewModel
        {
            CongressId = congressId,
            IsActive = true,
            DocumentTypes = await GetDocumentTypeItemsAsync(cancellationToken),
            Translations = await BuildEmptyTranslationViewModelsAsync(cancellationToken)
        };
    }

    private async Task<UpdateCongressDocumentViewModel> BuildUpdateViewModelAsync(
        GetCongressDocumentForUpdateResponse response,
        CancellationToken cancellationToken)
    {
        return new UpdateCongressDocumentViewModel
        {
            Id = response.Id,
            CongressId = response.CongressId,
            DocumentTypeId = response.DocumentTypeId,
            OriginalFileName = response.OriginalFileName,
            BucketName = response.BucketName,
            ObjectName = response.ObjectName,
            ContentType = response.ContentType,
            FileSize = response.FileSize,
            CoverImageFileName = response.CoverImageFileName,
            CoverImageBucketName = response.CoverImageBucketName,
            CoverImageObjectName = response.CoverImageObjectName,
            CoverImageContentType = response.CoverImageContentType,
            CoverImageFileSize = response.CoverImageFileSize,
            CoverImageUrl = BuildPublicAssetUrl(response.CoverImageBucketName, response.CoverImageObjectName),
            IsActive = response.IsActive,
            DocumentTypes = await GetDocumentTypeItemsAsync(cancellationToken),
            Translations = await BuildTranslationViewModelsAsync(response.Translations, cancellationToken)
        };
    }

    private async Task<List<DocumentTypeSelectItemViewModel>> GetDocumentTypeItemsAsync(CancellationToken cancellationToken)
    {
        string culture = await ResolveCurrentCultureAsync(cancellationToken);

        var documentTypes = await _mediator.Send(
            new GetListDocumentTypeQuery
            {
                Culture = culture,
                IsActive = true,
                SortColumn = "order",
                SortDirection = "asc",
                PageRequest = new PageRequest { Page = 0, PageSize = 500 }
            },
            cancellationToken);

        return documentTypes.Items
            .OrderBy(item => item.Order)
            .ThenBy(item => item.Name)
            .Select(item => new DocumentTypeSelectItemViewModel
            {
                Id = item.Id,
                Name = item.Name
            })
            .ToList();
    }


    private void ValidateCreateModel(CreateCongressDocumentViewModel model)
    {
        ValidateBaseModel(model.CongressId, model.DocumentTypeId, model.File, isFileRequired: true);
        ValidateTranslations(model.Translations);
        ValidateCoverImage(model.CoverImage);
    }

    private void ValidateUpdateModel(UpdateCongressDocumentViewModel model)
    {
        if (model.Id == Guid.Empty)
            ModelState.AddModelError(nameof(model.Id), GetText("Common.InvalidRequest", "Geçersiz istek."));

        ValidateBaseModel(model.CongressId, model.DocumentTypeId, model.File, isFileRequired: false);
        ValidateTranslations(model.Translations);
        ValidateCoverImage(model.CoverImage);
    }

    private void ValidateBaseModel(Guid congressId, Guid? documentTypeId, IFormFile? file, bool isFileRequired)
    {
        if (congressId == Guid.Empty)
            ModelState.AddModelError("CongressId", GetText("BackOffice.CongressDocuments.Validation.CongressRequired", "Kongre bilgisi zorunludur."));

        if (!documentTypeId.HasValue || documentTypeId.Value == Guid.Empty)
            ModelState.AddModelError("DocumentTypeId", GetText("BackOffice.CongressDocuments.Validation.DocumentTypeRequired", "Doküman tipi seçimi zorunludur."));


        if (file is null || file.Length <= 0)
        {
            if (isFileRequired)
                ModelState.AddModelError("File", GetText("BackOffice.CongressDocuments.Validation.FileRequired", "Dosya yüklenmesi zorunludur."));

            return;
        }

        if (file.Length > MaxFileSizeBytes)
            ModelState.AddModelError("File", GetText("BackOffice.CongressDocuments.Validation.FileTooLarge", "Dosya boyutu en fazla 50 MB olabilir."));

        string extension = Path.GetExtension(file.FileName);

        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            ModelState.AddModelError("File", GetText("BackOffice.CongressDocuments.Validation.FileInvalid", "Dosya geçersiz veya dosya türüne izin verilmiyor."));
    }


    private async Task<List<CongressDocumentTranslationViewModel>> BuildEmptyTranslationViewModelsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ApplicationLanguageDto> languages = await _applicationLanguageProvider.GetActiveLanguagesAsync(cancellationToken);

        return languages
            .OrderByDescending(language => language.IsDefault)
            .ThenBy(language => language.Name)
            .Select(language => new CongressDocumentTranslationViewModel
            {
                LanguageId = language.Id,
                Culture = language.Culture,
                LanguageName = language.Name,
                IsDefault = language.IsDefault
            })
            .ToList();
    }

    private async Task<List<CongressDocumentTranslationViewModel>> BuildTranslationViewModelsAsync(
        IEnumerable<CongressDocumentTranslationForUpdateDto> existingTranslations,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ApplicationLanguageDto> languages = await _applicationLanguageProvider.GetActiveLanguagesAsync(cancellationToken);
        List<CongressDocumentTranslationForUpdateDto> existing = existingTranslations.ToList();

        return languages
            .OrderByDescending(language => language.IsDefault)
            .ThenBy(language => language.Name)
            .Select(language =>
            {
                CongressDocumentTranslationForUpdateDto? translation = existing
                    .FirstOrDefault(item => item.LanguageId == language.Id);

                return new CongressDocumentTranslationViewModel
                {
                    LanguageId = language.Id,
                    Culture = language.Culture,
                    LanguageName = language.Name,
                    IsDefault = language.IsDefault,
                    Exists = translation is not null,
                    Description = translation?.Description
                };
            })
            .ToList();
    }

    private ICollection<TranslationInputDto> BuildTranslationInputs(
        IEnumerable<CongressDocumentTranslationViewModel> translations)
    {
        return translations
            .GroupBy(translation => translation.LanguageId)
            .Select(group => group.First())
            .Select(translation => new TranslationInputDto
            {
                LanguageId = translation.LanguageId,
                Fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Description"] = NormalizeText(translation.Description)
                }
            })
            .ToList();
    }

    private void ValidateTranslations(List<CongressDocumentTranslationViewModel> translations)
    {
        const int maxLength = 1000;

        for (int index = 0; index < translations.Count; index++)
        {
            string? description = translations[index].Description;

            if (!string.IsNullOrWhiteSpace(description) && description.Length > maxLength)
            {
                ModelState.AddModelError(
                    $"Translations[{index}].Description",
                    GetText("BackOffice.CongressDocuments.Validation.DescriptionMaxLength", "Açıklama en fazla 1000 karakter olabilir."));
            }
        }
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private void ValidateCoverImage(IFormFile? coverImage)
    {
        if (coverImage is null || coverImage.Length <= 0)
            return;

        if (coverImage.Length > MaxCoverImageSizeBytes)
        {
            ModelState.AddModelError(
                "CoverImage",
                GetText("BackOffice.CongressDocuments.Validation.CoverImageTooLarge", "Kapak görseli en fazla 5 MB olabilir."));
        }

        string extension = Path.GetExtension(coverImage.FileName);

        if (string.IsNullOrWhiteSpace(extension) || !AllowedCoverImageExtensions.Contains(extension))
        {
            ModelState.AddModelError(
                "CoverImage",
                GetText("BackOffice.CongressDocuments.Validation.CoverImageInvalid", "Kapak görseli JPG, PNG veya WEBP formatında olmalıdır."));
        }
    }

    private async Task<string> ResolveCurrentCultureAsync(CancellationToken cancellationToken)
    {
        string? routeCulture = RouteData.Values["culture"]?.ToString();

        if (!string.IsNullOrWhiteSpace(routeCulture))
            return await NormalizeCultureFromApplicationLanguagesAsync(routeCulture, cancellationToken);

        string? pathCulture = HttpContext.Request.Path.Value?
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        return await NormalizeCultureFromApplicationLanguagesAsync(pathCulture, cancellationToken);
    }

    private async Task<string> NormalizeCultureFromApplicationLanguagesAsync(string? culture, CancellationToken cancellationToken)
    {
        IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _applicationLanguageProvider.GetActiveLanguagesAsync(cancellationToken);
        string? requestedCulture = culture?.Trim();

        if (!string.IsNullOrWhiteSpace(requestedCulture))
        {
            ApplicationLanguageDto? matchedLanguage = activeLanguages
                .OrderByDescending(language => language.IsDefault)
                .ThenBy(language => language.Name)
                .FirstOrDefault(language =>
                    string.Equals(language.Culture, requestedCulture, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(GetTwoLetterIsoCode(language.Culture), requestedCulture, StringComparison.OrdinalIgnoreCase));

            if (matchedLanguage is not null)
                return matchedLanguage.Culture;
        }

        ApplicationLanguageDto defaultLanguage = await _applicationLanguageProvider.GetDefaultLanguageAsync(cancellationToken);

        return !string.IsNullOrWhiteSpace(defaultLanguage.Culture)
            ? defaultLanguage.Culture
            : activeLanguages.OrderByDescending(language => language.IsDefault).ThenBy(language => language.Name).FirstOrDefault()?.Culture ?? "tr-TR";
    }

    private static string GetTwoLetterIsoCode(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
            return string.Empty;

        string normalizedCulture = culture.Trim();
        int separatorIndex = normalizedCulture.IndexOf('-');

        return separatorIndex > 0 ? normalizedCulture[..separatorIndex] : normalizedCulture;
    }

    private string GetText(string key, string fallback)
    {
        string value = _localizer.GetStringValue(key);

        return string.IsNullOrWhiteSpace(value) ? key : value;
    }

    private static string? BuildPublicAssetUrl(string? bucketName, string? objectName)
    {
        if (string.IsNullOrWhiteSpace(bucketName) || string.IsNullOrWhiteSpace(objectName))
            return null;

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

    private static string ResolveDownloadFileName(GetByIdCongressDocumentResponse document)
    {
        string? originalFileName = NormalizeFileName(document.OriginalFileName);
        if (!string.IsNullOrWhiteSpace(originalFileName))
            return originalFileName;

        string objectFileName = Path.GetFileName(document.ObjectName?.Replace('\\', '/'));
        if (!string.IsNullOrWhiteSpace(objectFileName))
            return objectFileName;

        string extension = !string.IsNullOrWhiteSpace(document.FileExtension)
            ? document.FileExtension.Trim()
            : string.Empty;

        if (!string.IsNullOrWhiteSpace(extension) && !extension.StartsWith(".", StringComparison.Ordinal))
            extension = $".{extension}";

        return $"congress-document-{document.Id:N}{extension}";
    }

    private static string? NormalizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        string normalized = Path.GetFileName(fileName.Trim().Replace('\\', '/'));

        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }

    private static string ResolveDownloadContentType(string? contentType, string? objectName, string? originalFileName)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
            return contentType.Trim();

        string extension = Path.GetExtension(originalFileName);

        if (string.IsNullOrWhiteSpace(extension))
            extension = Path.GetExtension(objectName);

        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }

    private string GetExceptionMessage(Exception exception)
    {
        return !string.IsNullOrWhiteSpace(exception.Message)
            ? GetText(exception.Message, exception.Message)
            : GetText("Common.GenericError", string.Empty);
    }

    private object CreateValidationErrorResponse()
    {
        return new
        {
            success = false,
            message = GetText("Common.InvalidRequest", "Form alanlarını kontrol edin."),
            errors = ModelState
                .Where(item => item.Value?.Errors.Count > 0)
                .ToDictionary(
                    item => item.Key,
                    item => item.Value!.Errors.Select(error =>
                        GetText(string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Common.InvalidRequest" : error.ErrorMessage, error.ErrorMessage)).ToArray())
        };
    }

    private static string FormatFileSize(long? size)
    {
        if (!size.HasValue || size.Value <= 0)
            return "-";

        double bytes = size.Value;
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
