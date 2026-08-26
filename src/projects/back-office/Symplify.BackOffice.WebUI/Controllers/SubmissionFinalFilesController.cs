using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Claims;
using MediatR;
using Core.Application.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Common.Storage;
using Symplify.BackOffice.Application.Features.FullTextBook.Queries.GetWord;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Application.Services.Urls;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Submission;
using Symplify.BackOffice.Domain.ShortLinks;
using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.WebUI.Models.Shared.DataTables;
using Symplify.BackOffice.WebUI.Models.SubmissionFinalFiles;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture?}/submission-management/final-files")]
public sealed class SubmissionFinalFilesController : Controller
{
    private const int DefaultPageSize = 10;
    private const int MaxZipFileCount = 500;
    private const int MaxFullTextBookCoverSizeBytes = 8 * 1024 * 1024;

    private readonly IMediator _mediator;
    private readonly ISubmissionFileRepository _submissionFileRepository;
    private readonly ICongressRepository _congressRepository;
    private readonly IShortLinkRepository _shortLinkRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ObjectStorageOptions _objectStorageOptions;
    private readonly IBackOfficeViewLocalizer _localizer;
    private readonly ILogger<SubmissionFinalFilesController> _logger;
    private readonly IPublicUrlService _publicUrlService;

    public SubmissionFinalFilesController(
        IMediator mediator,
        ISubmissionFileRepository submissionFileRepository,
        ICongressRepository congressRepository,
        IShortLinkRepository shortLinkRepository,
        IObjectStorageService objectStorageService,
        IOptions<ObjectStorageOptions> objectStorageOptions,
        IBackOfficeViewLocalizer localizer,
        ILogger<SubmissionFinalFilesController> logger,
        IPublicUrlService publicUrlService)
    {
        _mediator = mediator;
        _submissionFileRepository = submissionFileRepository;
        _congressRepository = congressRepository;
        _shortLinkRepository = shortLinkRepository;
        _objectStorageService = objectStorageService;
        _objectStorageOptions = objectStorageOptions.Value;
        _localizer = localizer;
        _logger = logger;
        _publicUrlService = publicUrlService;
    }

    [HttpGet("full-text-book")]
    public async Task<IActionResult> FullTextBook(
        [FromQuery] bool archiveMode = false,
        CancellationToken cancellationToken = default)
    {
        return View(await BuildIndexModelAsync(
            SubmissionFileKind.FullText,
            isVideoPage: false,
            sourceAction: nameof(GetFullTextBookList),
            pageAction: nameof(FullTextBook),
            archiveMode: archiveMode,
            titleKey: "BackOffice.Submissions.FinalFiles.FullTextBook.PageTitle",
            listTitleKey: "BackOffice.Submissions.FinalFiles.FullTextBook.ListTitle",
            listDescriptionKey: "BackOffice.Submissions.FinalFiles.FullTextBook.ListDescription",
            cancellationToken));
    }

    [HttpGet("video-presentations")]
    public async Task<IActionResult> VideoPresentations(
        [FromQuery] bool archiveMode = false,
        CancellationToken cancellationToken = default)
    {
        return View(await BuildIndexModelAsync(
            SubmissionFileKind.Presentation,
            isVideoPage: true,
            sourceAction: nameof(GetVideoPresentationList),
            pageAction: nameof(VideoPresentations),
            archiveMode: archiveMode,
            titleKey: "BackOffice.Submissions.FinalFiles.VideoPresentations.PageTitle",
            listTitleKey: "BackOffice.Submissions.FinalFiles.VideoPresentations.ListTitle",
            listDescriptionKey: "BackOffice.Submissions.FinalFiles.VideoPresentations.ListDescription",
            cancellationToken));
    }

    [HttpPost("full-text-book/get-list")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> GetFullTextBookList(
        [FromForm] DataTableRequest request,
        [FromForm] bool archiveMode,
        CancellationToken cancellationToken)
        => GetFinalFileListAsync(request, SubmissionFileKind.FullText, archiveMode, cancellationToken);

    [HttpPost("full-text-book/generate-word")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateFullTextBookWord(
        [FromForm] Guid congressId,
        [FromForm] IFormFile? coverImage,
        CancellationToken cancellationToken)
    {
        if (!CanCurrentUserManageFinalFiles())
            return Forbid();

        if (congressId == Guid.Empty)
        {
            return BadRequest(new
            {
                success = false,
                message = T(
                    "BackOffice.Submissions.FinalFiles.FullTextBook.CongressRequired",
                    "Tam metin kitabı oluşturmak için bir kongre seçin.")
            });
        }

        try
        {
            (byte[]? coverBytes, string? coverContentType) =
                await ReadFullTextBookCoverAsync(coverImage, cancellationToken);

            var response = await _mediator.Send(new GetFullTextBookWordQuery
            {
                CongressId = congressId,
                Culture = RouteData.Values["culture"]?.ToString(),
                CoverImageBytes = coverBytes,
                CoverImageContentType = coverContentType
            }, cancellationToken);

            Response.Headers["Cache-Control"] = "no-store, no-cache, max-age=0";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return File(
                response.Content,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                response.FileName);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new
            {
                success = false,
                message = exception.Message
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Full text book Word generation failed for CongressId {CongressId}. TraceId: {TraceId}",
                congressId,
                HttpContext.TraceIdentifier);

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = T(
                    "BackOffice.Submissions.FinalFiles.FullTextBook.GenerationFailed",
                    "Tam metin kitabı oluşturulamadı.")
            });
        }
    }

    [HttpPost("video-presentations/get-list")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> GetVideoPresentationList(
        [FromForm] DataTableRequest request,
        [FromForm] bool archiveMode,
        CancellationToken cancellationToken)
        => GetFinalFileListAsync(request, SubmissionFileKind.Presentation, archiveMode, cancellationToken);

    [HttpPost("review")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review([FromForm] ReviewSubmissionFileRequest request, CancellationToken cancellationToken)
    {
        if (!CanCurrentUserManageFinalFiles())
            return Forbid();

        if (request.FileId == Guid.Empty)
            return BadRequest(new { success = false, message = T("BackOffice.Submissions.FinalFiles.Message.FileNotFound", "Dosya bulunamadı.") });

        SubmissionFile? file = await LoadFinalSubmissionFileForUpdateAsync(request.FileId, cancellationToken);
        if (file is null)
            return NotFound(new { success = false, message = T("BackOffice.Submissions.FinalFiles.Message.FileNotFound", "Dosya bulunamadı.") });

        if (!IsSupportedFinalFileKind(file.FileKind))
            return BadRequest(new { success = false, message = T("BackOffice.Submissions.FinalFiles.Message.UnsupportedFileType", "Bu dosya tipi final dosya incelemesi için uygun değildir.") });

        if (!IsAllowedReviewStatus(request.ReviewStatus))
            return BadRequest(new { success = false, message = T("BackOffice.Submissions.FinalFiles.Message.InvalidReviewStatus", "Geçersiz inceleme durumu.") });

        if ((request.ReviewStatus == SubmissionFileReviewStatus.Rejected || request.ReviewStatus == SubmissionFileReviewStatus.RevisionRequested) &&
            string.IsNullOrWhiteSpace(request.ReviewNote))
        {
            return BadRequest(new { success = false, message = T("BackOffice.Submissions.FinalFiles.Message.NoteRequired", "Reddetme veya revizyon isteği için açıklama girilmelidir.") });
        }

        ApplyReview(file, request.ReviewStatus, request.ReviewNote);
        await _submissionFileRepository.UpdateAsync(file);
        await SynchronizePresentationShortLinksAsync(file, cancellationToken);

        return Json(new
        {
            success = true,
            message = ResolveReviewSuccessMessage(request.ReviewStatus)
        });
    }

    [HttpPost("bulk-review")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkReview([FromForm] BulkReviewSubmissionFilesRequest request, CancellationToken cancellationToken)
    {
        if (!CanCurrentUserManageFinalFiles())
            return Forbid();

        List<Guid> fileIds = request.FileIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (fileIds.Count == 0)
            return BadRequest(new { success = false, message = T("BackOffice.Submissions.FinalFiles.Message.SelectAtLeastOne", "Lütfen en az bir dosya seçin.") });

        if (!IsAllowedReviewStatus(request.ReviewStatus))
            return BadRequest(new { success = false, message = T("BackOffice.Submissions.FinalFiles.Message.InvalidReviewStatus", "Geçersiz inceleme durumu.") });

        List<SubmissionFile> files = await _submissionFileRepository
            .Query()
            .Include(item => item.Submission)
            .Where(item => fileIds.Contains(item.Id) && item.DeletedDate == null && item.IsActive)
            .ToListAsync(cancellationToken);

        foreach (SubmissionFile file in files.Where(file => IsSupportedFinalFileKind(file.FileKind)))
        {
            ApplyReview(file, request.ReviewStatus, request.ReviewNote);
            await _submissionFileRepository.UpdateAsync(file);
            await SynchronizePresentationShortLinksAsync(file, cancellationToken);
        }

        return Json(new
        {
            success = true,
            message = ResolveReviewSuccessMessage(request.ReviewStatus)
        });
    }

    [HttpPost("toggle-program-book")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleProgramBook([FromForm] ToggleProgramBookFileRequest request, CancellationToken cancellationToken)
    {
        if (!CanCurrentUserManageFinalFiles())
            return Forbid();

        SubmissionFile? file = await LoadFinalSubmissionFileForUpdateAsync(request.FileId, cancellationToken);
        if (file is null)
            return NotFound(new { success = false, message = T("BackOffice.Submissions.FinalFiles.Message.FileNotFound", "Dosya bulunamadı.") });

        if (file.FileKind != SubmissionFileKind.Presentation)
            return BadRequest(new { success = false, message = T("BackOffice.Submissions.FinalFiles.Message.OnlyVideoProgramBook", "Program kitabına yalnızca video sunum dosyaları eklenebilir.") });

        if (request.IsIncludedInProgramBook && file.ReviewStatus != SubmissionFileReviewStatus.Approved)
            return BadRequest(new { success = false, message = T("BackOffice.Submissions.FinalFiles.Message.ApprovalRequiredForProgramBook", "Program kitabına eklemek için video önce onaylanmalıdır.") });

        file.IsIncludedInProgramBook = request.IsIncludedInProgramBook;
        file.UpdatedDate = DateTime.UtcNow;
        file.UpdatedBy = GetCurrentUserId()?.ToString() ?? "SubmissionFinalFiles";
        await _submissionFileRepository.UpdateAsync(file);
        await SynchronizePresentationShortLinksAsync(file, cancellationToken);

        return Json(new
        {
            success = true,
            message = request.IsIncludedInProgramBook
                ? T("BackOffice.Submissions.FinalFiles.Message.ProgramBookIncluded", "Video program kitabına eklendi.")
                : T("BackOffice.Submissions.FinalFiles.Message.ProgramBookExcluded", "Video program kitabından çıkarıldı.")
        });
    }

    [HttpPost("delete-full-text")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFullText(
        [FromForm] DeleteSubmissionFinalFileRequest request,
        CancellationToken cancellationToken)
    {
        if (!CanCurrentUserManageFinalFiles())
            return Forbid();

        if (request.FileId == Guid.Empty)
        {
            return BadRequest(new
            {
                success = false,
                message = T("BackOffice.Submissions.FinalFiles.Message.FileNotFound", "Dosya bulunamadı.")
            });
        }

        SubmissionFile? file = await LoadFinalSubmissionFileForUpdateAsync(request.FileId, cancellationToken);
        if (file is null)
        {
            return NotFound(new
            {
                success = false,
                message = T("BackOffice.Submissions.FinalFiles.Message.FileNotFound", "Dosya bulunamadı.")
            });
        }

        if (file.FileKind != SubmissionFileKind.FullText)
        {
            return BadRequest(new
            {
                success = false,
                message = T(
                    "BackOffice.Submissions.FinalFiles.Message.OnlyFullTextDelete",
                    "Bu işlem yalnızca tam metin dosyaları için kullanılabilir.")
            });
        }

        string? bucketName = ResolveSubmissionBucketName();
        string objectName = file.FilePath?.Trim() ?? string.Empty;
        bool hasInternalStorageObject =
            !string.IsNullOrWhiteSpace(objectName) &&
            !BackOfficeObjectStorageHelper.IsExternalOrLegacyLocalPath(objectName);

        if (hasInternalStorageObject && string.IsNullOrWhiteSpace(bucketName))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = T(
                    "BackOffice.Submissions.FinalFiles.Message.StorageNotConfigured",
                    "Dosya depolama ayarı bulunamadığı için silme işlemi tamamlanamadı.")
            });
        }

        bool previousIsActive = file.IsActive;
        DateTime? previousDeletedDate = file.DeletedDate;
        string? previousDeletedBy = file.DeletedBy;
        DateTime? previousUpdatedDate = file.UpdatedDate;
        string? previousUpdatedBy = file.UpdatedBy;

        DateTime now = DateTime.UtcNow;
        string actor = GetCurrentUserId()?.ToString() ?? "SubmissionFinalFiles";

        file.IsActive = false;
        file.DeletedDate = now;
        file.DeletedBy = actor;
        file.UpdatedDate = now;
        file.UpdatedBy = actor;

        await _submissionFileRepository.UpdateAsync(file);

        try
        {
            if (hasInternalStorageObject)
            {
                await _objectStorageService.DeleteAsync(
                    new ObjectStorageDeleteRequest
                    {
                        BucketName = bucketName!,
                        ObjectName = objectName
                    },
                    cancellationToken);
            }
        }
        catch (Exception storageException)
        {
            _logger.LogError(
                storageException,
                "Full text object could not be deleted. FileId: {FileId}, Bucket: {BucketName}, ObjectName: {ObjectName}",
                file.Id,
                bucketName,
                objectName);

            file.IsActive = previousIsActive;
            file.DeletedDate = previousDeletedDate;
            file.DeletedBy = previousDeletedBy;
            file.UpdatedDate = previousUpdatedDate;
            file.UpdatedBy = previousUpdatedBy;

            try
            {
                await _submissionFileRepository.UpdateAsync(file);
            }
            catch (Exception compensationException)
            {
                _logger.LogCritical(
                    compensationException,
                    "Full text delete compensation failed. FileId: {FileId}",
                    file.Id);
            }

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = T(
                    "BackOffice.Submissions.FinalFiles.Message.DeleteFailed",
                    "Tam metin dosyası silinemedi. Kayıt korunmuştur.")
            });
        }

        return Json(new
        {
            success = true,
            message = T(
                "BackOffice.Submissions.FinalFiles.Message.Deleted",
                "Tam metin dosyası silindi.")
        });
    }


    [HttpPost("bulk-delete-full-text")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkDeleteFullText(
        [FromForm] BulkDeleteSubmissionFinalFilesRequest request,
        CancellationToken cancellationToken)
    {
        if (!CanCurrentUserManageFinalFiles())
            return Forbid();

        List<Guid> fileIds = request.FileIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .Take(MaxZipFileCount)
            .ToList();

        if (fileIds.Count == 0)
        {
            return BadRequest(new
            {
                success = false,
                message = T(
                    "BackOffice.Submissions.FinalFiles.Message.SelectAtLeastOne",
                    "Lütfen en az bir dosya seçin.")
            });
        }

        List<SubmissionFile> files = await _submissionFileRepository
            .Query()
            .Where(file =>
                fileIds.Contains(file.Id) &&
                file.DeletedDate == null &&
                file.IsActive)
            .ToListAsync(cancellationToken);

        if (files.Count == 0)
        {
            _logger.LogWarning(
                "Bulk full text delete did not resolve any active files. RequestedFileIds: {RequestedFileIds}",
                string.Join(",", fileIds));

            return NotFound(new
            {
                success = false,
                message = T(
                    "BackOffice.Submissions.FinalFiles.Message.FileNotFound",
                    "Seçilen dosyalar bulunamadı veya daha önce silinmiş.")
            });
        }

        List<SubmissionFile> fullTextFiles = files
            .Where(file => file.FileKind == SubmissionFileKind.FullText)
            .ToList();

        int missingCount = fileIds.Count - files.Count;
        int unsupportedCount = files.Count - fullTextFiles.Count;

        if (fullTextFiles.Count == 0)
        {
            return BadRequest(new
            {
                success = false,
                message = T(
                    "BackOffice.Submissions.FinalFiles.Message.OnlyFullTextDelete",
                    "Bu işlem yalnızca tam metin dosyaları için kullanılabilir.")
            });
        }

        string? bucketName = ResolveSubmissionBucketName();
        bool storageIsRequired = fullTextFiles.Any(file =>
        {
            string objectName = file.FilePath?.Trim() ?? string.Empty;
            return
                !string.IsNullOrWhiteSpace(objectName) &&
                !BackOfficeObjectStorageHelper.IsExternalOrLegacyLocalPath(objectName);
        });

        if (storageIsRequired && string.IsNullOrWhiteSpace(bucketName))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = T(
                    "BackOffice.Submissions.FinalFiles.Message.StorageNotConfigured",
                    "Dosya depolama ayarı bulunamadığı için silme işlemi tamamlanamadı.")
            });
        }

        int deletedCount = 0;
        int failedCount = 0;
        string actor = GetCurrentUserId()?.ToString() ?? "SubmissionFinalFiles";
        DateTime now = DateTime.UtcNow;

        foreach (SubmissionFile file in fullTextFiles)
        {
            bool previousIsActive = file.IsActive;
            DateTime? previousDeletedDate = file.DeletedDate;
            string? previousDeletedBy = file.DeletedBy;
            DateTime? previousUpdatedDate = file.UpdatedDate;
            string? previousUpdatedBy = file.UpdatedBy;

            string objectName = file.FilePath?.Trim() ?? string.Empty;
            bool hasInternalStorageObject =
                !string.IsNullOrWhiteSpace(objectName) &&
                !BackOfficeObjectStorageHelper.IsExternalOrLegacyLocalPath(objectName);

            bool databaseMarkedDeleted = false;

            try
            {
                file.IsActive = false;
                file.DeletedDate = now;
                file.DeletedBy = actor;
                file.UpdatedDate = now;
                file.UpdatedBy = actor;

                await _submissionFileRepository.UpdateAsync(file);
                databaseMarkedDeleted = true;

                if (hasInternalStorageObject)
                {
                    await _objectStorageService.DeleteAsync(
                        new ObjectStorageDeleteRequest
                        {
                            BucketName = bucketName!,
                            ObjectName = objectName
                        },
                        cancellationToken);
                }

                deletedCount++;
            }
            catch (Exception deleteException)
            {
                failedCount++;

                _logger.LogError(
                    deleteException,
                    "Bulk full text delete failed. FileId: {FileId}, Bucket: {BucketName}, ObjectName: {ObjectName}, DatabaseMarkedDeleted: {DatabaseMarkedDeleted}",
                    file.Id,
                    bucketName,
                    objectName,
                    databaseMarkedDeleted);

                if (!databaseMarkedDeleted)
                    continue;

                file.IsActive = previousIsActive;
                file.DeletedDate = previousDeletedDate;
                file.DeletedBy = previousDeletedBy;
                file.UpdatedDate = previousUpdatedDate;
                file.UpdatedBy = previousUpdatedBy;

                try
                {
                    await _submissionFileRepository.UpdateAsync(file);
                }
                catch (Exception compensationException)
                {
                    _logger.LogCritical(
                        compensationException,
                        "Bulk full text delete compensation failed. FileId: {FileId}",
                        file.Id);
                }
            }
        }

        int skippedCount = missingCount + unsupportedCount;

        string message = failedCount == 0
            ? string.Format(
                CultureInfo.CurrentUICulture,
                T(
                    "BackOffice.Submissions.FinalFiles.Message.BulkDeleted",
                    "{0} tam metin dosyası silindi."),
                deletedCount)
            : string.Format(
                CultureInfo.CurrentUICulture,
                T(
                    "BackOffice.Submissions.FinalFiles.Message.BulkDeletePartial",
                    "{0} dosya silindi, {1} dosya silinemedi, {2} kayıt atlandı."),
                deletedCount,
                failedCount,
                skippedCount);

        return Json(new
        {
            success = failedCount == 0,
            deletedCount,
            failedCount,
            skippedCount,
            message
        });
    }

    [HttpPost("download-selected")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DownloadSelected([FromForm] BulkDownloadSubmissionFilesRequest request, CancellationToken cancellationToken)
    {
        if (!CanCurrentUserManageFinalFiles())
            return Forbid();

        List<Guid> fileIds = request.FileIds.Where(id => id != Guid.Empty).Distinct().Take(MaxZipFileCount).ToList();
        if (fileIds.Count == 0)
            return BadRequest(T("BackOffice.Submissions.FinalFiles.Message.SelectAtLeastOne", "Lütfen en az bir dosya seçin."));

        string? bucketName = ResolveSubmissionBucketName();
        if (string.IsNullOrWhiteSpace(bucketName))
            return Problem("Submission file storage bucket is not configured.");

        List<SubmissionFile> files = await BuildFinalFilesBaseQuery()
            .Where(item => fileIds.Contains(item.Id))
            .OrderBy(item => item.Submission.SubmissionNumber)
            .ThenBy(item => item.OriginalFileName)
            .ToListAsync(cancellationToken);

        if (files.Count == 0)
            return NotFound(T("BackOffice.Submissions.FinalFiles.Message.FileNotFound", "Dosya bulunamadı."));

        string tempPath = Path.Combine(Path.GetTempPath(), $"symplify-final-files-{Guid.NewGuid():N}.zip");

        await using (FileStream zipFileStream = new(tempPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 1024 * 64, FileOptions.Asynchronous))
        {
            using ZipArchive archive = new(zipFileStream, ZipArchiveMode.Create, leaveOpen: true);

            foreach (SubmissionFile file in files)
            {
                if (string.IsNullOrWhiteSpace(file.FilePath) ||
                    file.FilePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    file.FilePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string entryName = BuildZipEntryName(file);
                ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);

                await using Stream objectStream = await _objectStorageService.OpenReadAsync(bucketName, file.FilePath, cancellationToken);
                await using Stream entryStream = entry.Open();
                await objectStream.CopyToAsync(entryStream, cancellationToken);
            }
        }

        FileStream downloadStream = new(
            tempPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            1024 * 64,
            FileOptions.Asynchronous | FileOptions.DeleteOnClose);

        string zipName = $"symplify-final-files-{DateTime.UtcNow:yyyyMMdd-HHmm}.zip";
        return File(downloadStream, "application/zip", zipName);
    }

    [HttpPost("public-links")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GetPublicLinks([FromForm] GenerateSubmissionFileShortLinksRequest request, CancellationToken cancellationToken)
    {
        if (!CanCurrentUserManageFinalFiles())
            return Forbid();

        List<Guid> fileIds = request.FileIds.Where(id => id != Guid.Empty).Distinct().Take(MaxZipFileCount).ToList();
        if (fileIds.Count == 0)
            return BadRequest(new { success = false, message = T("BackOffice.Submissions.FinalFiles.Message.SelectAtLeastOne", "Lütfen en az bir dosya seçin.") });

        List<SubmissionFile> files = await BuildFinalFilesBaseQuery()
            .Where(item => fileIds.Contains(item.Id))
            .OrderBy(item => item.Submission.SubmissionNumber)
            .ThenBy(item => item.OriginalFileName)
            .ToListAsync(cancellationToken);

        List<string> links = new();
        foreach (SubmissionFile file in files)
        {
            if (file.FileKind != SubmissionFileKind.Presentation)
                continue;

            if (!CanCreatePublicVideoLink(file))
                continue;

            ShortLink shortLink = await EnsureShortLinkAsync(file, cancellationToken);
            links.Add(BuildShortLinkUrl(shortLink.Code));
        }

        if (links.Count == 0)
        {
            return BadRequest(new
            {
                success = false,
                message = T("BackOffice.Submissions.FinalFiles.Message.ShortLinkUnavailable", "Kısa link oluşturulabilecek onaylı video bulunamadı.")
            });
        }

        return Json(new
        {
            success = true,
            links,
            message = T("BackOffice.Submissions.FinalFiles.Message.ShortLinkCreated", "Kısa link oluşturuldu.")
        });
    }

    private async Task<IActionResult> GetFinalFileListAsync(
        DataTableRequest request,
        SubmissionFileKind fileKind,
        bool archiveMode,
        CancellationToken cancellationToken)
    {
        DataTableQueryOptions tableOptions = DataTableQueryOptions.From(
            request,
            defaultSortColumn: "uploadedAt",
            defaultSortDirection: "desc",
            allowedSortColumns: new[]
            {
                "submissionNumber",
                "title",
                "author",
                "fileName",
                "reviewStatus",
                "uploadedAt"
            });

        CongressStatus targetCongressStatus = archiveMode
            ? CongressStatus.Archived
            : CongressStatus.Published;

        IQueryable<SubmissionFile> query = BuildFinalFilesBaseQuery()
            .Where(item =>
                item.FileKind == fileKind &&
                item.Submission.Congress.DeletedDate == null &&
                item.Submission.Congress.Status == targetCongressStatus);

        Guid? congressId = ParseNullableGuid(Request.Form["CongressId"].ToString());
        if (congressId.HasValue)
            query = query.Where(item => item.Submission.CongressId == congressId.Value);

        int totalCount = await query.CountAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(tableOptions.SearchText))
            query = ApplySearch(query, tableOptions.SearchText);

        int filteredCount = await query.CountAsync(cancellationToken);

        List<SubmissionFile> files = await ApplySorting(query, tableOptions.SortColumn, tableOptions.SortDirection)
            .Skip(tableOptions.Start)
            .Take(tableOptions.PageSize <= 0 ? DefaultPageSize : tableOptions.PageSize)
            .ToListAsync(cancellationToken);

        Dictionary<Guid, string> videoPlayerUrls = await BuildVideoPlayerUrlMapAsync(
            files,
            fileKind,
            cancellationToken);

        List<object> rows = files
            .Select((file, index) => ToDataTableRow(
                file,
                tableOptions.Start + index + 1,
                videoPlayerUrls.TryGetValue(file.Id, out string? videoPlayerUrl) ? videoPlayerUrl : null))
            .Cast<object>()
            .ToList();

        return Json(new
        {
            draw = request.Draw,
            recordsTotal = totalCount,
            recordsFiltered = filteredCount,
            data = rows
        });
    }

    private IQueryable<SubmissionFile> BuildFinalFilesBaseQuery()
    {
        return _submissionFileRepository
            .Query()
            .AsNoTracking()
            .Include(item => item.Submission)
                .ThenInclude(submission => submission.Congress)
                    .ThenInclude(congress => congress.Translations)
                        .ThenInclude(translation => translation.Language)
            .Include(item => item.Submission)
                .ThenInclude(submission => submission.SubmissionType)
                    .ThenInclude(type => type!.Translations)
                        .ThenInclude(translation => translation.Language)
            .Include(item => item.Submission)
                .ThenInclude(submission => submission.Authors)
                    .ThenInclude(author => author.Title)
                        .ThenInclude(title => title!.Translations)
                            .ThenInclude(translation => translation.Language)
            .Where(item =>
                item.DeletedDate == null &&
                item.IsActive &&
                (item.FileKind == SubmissionFileKind.FullText || item.FileKind == SubmissionFileKind.Presentation));
    }

    private async Task<SubmissionFile?> LoadFinalSubmissionFileForUpdateAsync(Guid fileId, CancellationToken cancellationToken)
    {
        return await _submissionFileRepository
            .Query()
            .Include(item => item.Submission)
            .FirstOrDefaultAsync(item => item.Id == fileId && item.DeletedDate == null && item.IsActive, cancellationToken);
    }

    private static IQueryable<SubmissionFile> ApplySearch(IQueryable<SubmissionFile> query, string searchText)
    {
        string keyword = searchText.Trim();

        return query.Where(item =>
            item.Submission.SubmissionNumber.Contains(keyword) ||
            item.Submission.Title.Contains(keyword) ||
            (item.Submission.TitleEn != null && item.Submission.TitleEn.Contains(keyword)) ||
            item.OriginalFileName.Contains(keyword) ||
            item.Submission.Authors.Any(author =>
                author.FirstName.Contains(keyword) ||
                author.LastName.Contains(keyword) ||
                (author.Email != null && author.Email.Contains(keyword))));
    }

    private static IQueryable<SubmissionFile> ApplySorting(IQueryable<SubmissionFile> query, string sortColumn, string sortDirection)
    {
        bool desc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        string normalized = NormalizeSortColumn(sortColumn);

        return normalized switch
        {
            "submissionnumber" => desc
                ? query.OrderByDescending(item => item.Submission.SubmissionNumber)
                : query.OrderBy(item => item.Submission.SubmissionNumber),
            "title" => desc
                ? query.OrderByDescending(item => item.Submission.Title)
                : query.OrderBy(item => item.Submission.Title),
            "author" => desc
                ? query.OrderByDescending(item => item.Submission.Authors.Where(author => author.IsCorrespondingAuthor).Select(author => author.LastName).FirstOrDefault())
                : query.OrderBy(item => item.Submission.Authors.Where(author => author.IsCorrespondingAuthor).Select(author => author.LastName).FirstOrDefault()),
            "filename" => desc
                ? query.OrderByDescending(item => item.OriginalFileName)
                : query.OrderBy(item => item.OriginalFileName),
            "reviewstatus" => desc
                ? query.OrderByDescending(item => item.ReviewStatus)
                : query.OrderBy(item => item.ReviewStatus),
            _ => desc
                ? query.OrderByDescending(item => item.CreatedDate)
                : query.OrderBy(item => item.CreatedDate)
        };
    }

    private object ToDataTableRow(SubmissionFile file, int rowNumber, string? videoPlayerUrl = null)
    {
        Submission submission = file.Submission;
        DateTime uploadedAt = file.CreatedDate;
        string publicUrl = BuildPublicFileUrl(file);
        string? previewUrl = !string.IsNullOrWhiteSpace(videoPlayerUrl)
            ? videoPlayerUrl
            : Url.Action("PreviewFile", "SubmissionManagementFiles", new { culture = RouteData.Values["culture"]?.ToString(), fileId = file.Id });

        return new
        {
            id = file.Id,
            fileKind = file.FileKind.ToString(),
            submissionId = submission.Id,
            rowNumber,
            submissionNumber = string.IsNullOrWhiteSpace(submission.SubmissionNumber) ? submission.Id.ToString("N")[..8].ToUpperInvariant() : submission.SubmissionNumber,
            title = ResolveTitle(submission),
            submissionTypeName = ResolveSubmissionTypeName(submission),
            congressName = ResolveCongressName(submission),
            correspondingAuthorName = ResolveCorrespondingAuthorName(submission),
            otherAuthorsText = ResolveOtherAuthorsText(submission),
            authorCount = submission.Authors.Count,
            originalFileName = file.OriginalFileName,
            fileExtension = ResolveFileExtension(file.OriginalFileName),
            fileSizeText = FormatFileSize(file.FileSize),
            reviewStatus = file.ReviewStatus.ToString(),
            reviewStatusText = ResolveReviewStatusText(file.ReviewStatus),
            reviewStatusBadgeClass = ResolveReviewStatusBadgeClass(file.ReviewStatus),
            isApproved = file.ReviewStatus == SubmissionFileReviewStatus.Approved,
            isIncludedInProgramBook = file.IsIncludedInProgramBook,
            uploadedDate = uploadedAt.ToString("dd.MM.yyyy", CultureInfo.CurrentUICulture),
            uploadedTime = uploadedAt.ToString("HH:mm", CultureInfo.CurrentUICulture),
            downloadUrl = Url.Action("DownloadFile", "SubmissionManagementFiles", new { culture = RouteData.Values["culture"]?.ToString(), fileId = file.Id }),
            previewUrl,
            watchUrl = videoPlayerUrl,
            publicUrl = !string.IsNullOrWhiteSpace(videoPlayerUrl) ? videoPlayerUrl : publicUrl
        };
    }

    private async Task<Dictionary<Guid, string>> BuildVideoPlayerUrlMapAsync(
        IReadOnlyCollection<SubmissionFile> files,
        SubmissionFileKind fileKind,
        CancellationToken cancellationToken)
    {
        if (fileKind != SubmissionFileKind.Presentation || files.Count == 0)
            return new Dictionary<Guid, string>();

        List<SubmissionFile> eligibleFiles = files
            .Where(CanCreatePublicVideoLink)
            .ToList();

        if (eligibleFiles.Count == 0)
            return new Dictionary<Guid, string>();

        List<Guid> eligibleFileIds = eligibleFiles
            .Select(file => file.Id)
            .Distinct()
            .ToList();

        List<ShortLink> existingLinks = await _shortLinkRepository
            .Query()
            .Where(link =>
                link.TargetType == ShortLinkTargetType.SubmissionPresentationVideo &&
                eligibleFileIds.Contains(link.TargetId) &&
                link.DeletedDate == null &&
                link.IsActive)
            .ToListAsync(cancellationToken);

        Dictionary<Guid, ShortLink> linksByFileId = existingLinks
            .GroupBy(link => link.TargetId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(link => link.CreatedDate).First());

        foreach (SubmissionFile file in eligibleFiles)
        {
            if (linksByFileId.ContainsKey(file.Id))
                continue;

            ShortLink shortLink = await EnsureShortLinkAsync(file, cancellationToken);
            linksByFileId[file.Id] = shortLink;
        }

        return linksByFileId.ToDictionary(
            pair => pair.Key,
            pair => BuildShortLinkUrl(pair.Value.Code));
    }

    private async Task<SubmissionFinalFilesIndexViewModel> BuildIndexModelAsync(
        SubmissionFileKind fileKind,
        bool isVideoPage,
        string sourceAction,
        string pageAction,
        bool archiveMode,
        string titleKey,
        string listTitleKey,
        string listDescriptionKey,
        CancellationToken cancellationToken)
    {
        string culture = RouteData.Values["culture"]?.ToString() ?? CultureInfo.CurrentUICulture.Name;

        return new SubmissionFinalFilesIndexViewModel
        {
            FileKind = fileKind,
            IsVideoPage = isVideoPage,
            PageTitleKey = titleKey,
            ListTitleKey = listTitleKey,
            ListDescriptionKey = listDescriptionKey,
            SourceUrl = Url.Action(sourceAction, "SubmissionFinalFiles", new { culture }) ?? string.Empty,
            ReviewUrl = Url.Action(nameof(Review), "SubmissionFinalFiles", new { culture }) ?? string.Empty,
            BulkReviewUrl = Url.Action(nameof(BulkReview), "SubmissionFinalFiles", new { culture }) ?? string.Empty,
            BulkDownloadUrl = Url.Action(nameof(DownloadSelected), "SubmissionFinalFiles", new { culture }) ?? string.Empty,
            GenerateFullTextBookUrl = Url.Action(nameof(GenerateFullTextBookWord), "SubmissionFinalFiles", new { culture }) ?? string.Empty,
            BulkDeleteUrl = Url.Action(nameof(BulkDeleteFullText), "SubmissionFinalFiles", new { culture }) ?? string.Empty,
            DeleteUrl = Url.Action(nameof(DeleteFullText), "SubmissionFinalFiles", new { culture }) ?? string.Empty,
            ToggleProgramBookUrl = Url.Action(nameof(ToggleProgramBook), "SubmissionFinalFiles", new { culture }) ?? string.Empty,
            PublicLinksUrl = Url.Action(nameof(GetPublicLinks), "SubmissionFinalFiles", new { culture }) ?? string.Empty,
            ArchiveMode = archiveMode,
            ArchiveToggleUrl = Url.Action(
                pageAction,
                "SubmissionFinalFiles",
                new
                {
                    culture,
                    archiveMode = !archiveMode
                }) ?? string.Empty,
            CongressOptions = await BuildCongressOptionsAsync(fileKind, archiveMode, cancellationToken)
        };
    }

    private async Task<IReadOnlyList<SelectListItem>> BuildCongressOptionsAsync(
        SubmissionFileKind fileKind,
        bool archiveMode,
        CancellationToken cancellationToken)
    {
        _ = fileKind;

        CongressStatus targetCongressStatus = archiveMode
            ? CongressStatus.Archived
            : CongressStatus.Published;

        List<Symplify.BackOffice.Domain.Congress.Congress> congresses = await _congressRepository
            .Query()
            .AsNoTracking()
            .Include(congress => congress.Translations)
                .ThenInclude(translation => translation.Language)
            .Where(congress =>
                congress.DeletedDate == null &&
                congress.Status == targetCongressStatus)
            .OrderBy(congress => congress.Code)
            .ThenByDescending(congress => congress.StartDate ?? congress.CreatedDate)
            .ToListAsync(cancellationToken);

        return congresses
            .Select(congress => new SelectListItem
            {
                Value = congress.Id.ToString(),
                Text = FormatCongressOptionText(congress)
            })
            .ToList();
    }

    private string FormatCongressOptionText(Symplify.BackOffice.Domain.Congress.Congress congress)
    {
        string congressName = ResolveCongressName(congress);
        return string.IsNullOrWhiteSpace(congress.Code)
            ? congressName
            : $"{congress.Code.Trim()} - {congressName}";
    }

    private void ApplyReview(SubmissionFile file, SubmissionFileReviewStatus reviewStatus, string? reviewNote)
    {
        DateTime now = DateTime.UtcNow;
        Guid? currentUserId = GetCurrentUserId();
        string actor = currentUserId?.ToString() ?? "SubmissionFinalFiles";

        file.ReviewStatus = reviewStatus;
        file.ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? null : reviewNote.Trim();
        file.ReviewedAt = now;
        file.ReviewedByUserId = currentUserId;
        file.UpdatedDate = now;
        file.UpdatedBy = actor;

        if (file.FileKind == SubmissionFileKind.Presentation)
        {
            file.IsIncludedInProgramBook = reviewStatus == SubmissionFileReviewStatus.Approved;
        }
        else if (reviewStatus != SubmissionFileReviewStatus.Approved)
        {
            file.IsIncludedInProgramBook = false;
        }
    }

    private string ResolveReviewSuccessMessage(SubmissionFileReviewStatus reviewStatus)
    {
        return reviewStatus switch
        {
            SubmissionFileReviewStatus.Approved => T("BackOffice.Submissions.FinalFiles.Message.Approved", "Dosya onaylandı."),
            SubmissionFileReviewStatus.Rejected => T("BackOffice.Submissions.FinalFiles.Message.Rejected", "Dosya reddedildi."),
            SubmissionFileReviewStatus.RevisionRequested => T("BackOffice.Submissions.FinalFiles.Message.RevisionRequested", "Dosya için revizyon istendi."),
            _ => T("BackOffice.Submissions.FinalFiles.Message.Reverted", "Dosya inceleme bekliyor durumuna alındı.")
        };
    }

    private async Task SynchronizePresentationShortLinksAsync(SubmissionFile file, CancellationToken cancellationToken)
    {
        if (file.FileKind != SubmissionFileKind.Presentation)
            return;

        if (CanCreatePublicVideoLink(file))
        {
            await EnsureShortLinkAsync(file, cancellationToken);
            return;
        }

        await DeactivateShortLinksAsync(file.Id, cancellationToken);
    }

    private async Task<ShortLink> EnsureShortLinkAsync(SubmissionFile file, CancellationToken cancellationToken)
    {
        ShortLink? existing = await _shortLinkRepository
            .Query()
            .FirstOrDefaultAsync(item =>
                item.TargetType == ShortLinkTargetType.SubmissionPresentationVideo &&
                item.TargetId == file.Id &&
                item.DeletedDate == null &&
                item.IsActive,
                cancellationToken);

        if (existing is not null)
            return existing;

        ShortLink shortLink = new()
        {
            Id = Guid.NewGuid(),
            Code = await GenerateUniqueShortCodeAsync(cancellationToken),
            TargetType = ShortLinkTargetType.SubmissionPresentationVideo,
            TargetId = file.Id,
            Culture = RouteData.Values["culture"]?.ToString() ?? CultureInfo.CurrentUICulture.Name,
            IsActive = true,
            ClickCount = 0,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = GetCurrentUserId()?.ToString() ?? "SubmissionFinalFiles"
        };

        await _shortLinkRepository.AddAsync(shortLink);
        return shortLink;
    }

    private async Task DeactivateShortLinksAsync(Guid fileId, CancellationToken cancellationToken)
    {
        List<ShortLink> links = await _shortLinkRepository
            .Query()
            .Where(item =>
                item.TargetType == ShortLinkTargetType.SubmissionPresentationVideo &&
                item.TargetId == fileId &&
                item.DeletedDate == null &&
                item.IsActive)
            .ToListAsync(cancellationToken);

        if (links.Count == 0)
            return;

        DateTime now = DateTime.UtcNow;
        string actor = GetCurrentUserId()?.ToString() ?? "SubmissionFinalFiles";

        foreach (ShortLink link in links)
        {
            link.IsActive = false;
            link.UpdatedDate = now;
            link.UpdatedBy = actor;
            await _shortLinkRepository.UpdateAsync(link);
        }
    }

    private async Task<string> GenerateUniqueShortCodeAsync(CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            string code = GenerateShortCode();
            bool exists = await _shortLinkRepository
                .Query()
                .AnyAsync(item => item.Code == code && item.DeletedDate == null, cancellationToken);

            if (!exists)
                return code;
        }

        return Guid.NewGuid().ToString("N")[..12];
    }

    private static string GenerateShortCode(int length = 7)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
        Span<char> buffer = stackalloc char[length];

        for (int index = 0; index < buffer.Length; index++)
            buffer[index] = chars[RandomNumberGenerator.GetInt32(chars.Length)];

        return new string(buffer);
    }

    private bool CanCreatePublicVideoLink(SubmissionFile file)
    {
        return file.FileKind == SubmissionFileKind.Presentation &&
               file.IsActive &&
               file.DeletedDate == null &&
               file.ReviewStatus == SubmissionFileReviewStatus.Approved &&
               file.IsIncludedInProgramBook;
    }

    private string BuildShortLinkUrl(string code)
    {
        return _publicUrlService.Build($"/v/{Uri.EscapeDataString(code)}");
    }

    private string BuildPublicFileUrl(SubmissionFile file)
    {
        string culture = RouteData.Values["culture"]?.ToString() ?? CultureInfo.CurrentUICulture.Name;
        string submissionNumber = string.IsNullOrWhiteSpace(file.Submission.SubmissionNumber)
            ? file.SubmissionId.ToString("N")[..8]
            : file.Submission.SubmissionNumber.Trim();

        string path = file.FileKind == SubmissionFileKind.Presentation
            ? $"/{culture}/program/videos/{Uri.EscapeDataString(submissionNumber)}"
            : $"/{culture}/full-text-book/{Uri.EscapeDataString(submissionNumber)}";

        return _publicUrlService.Build(path);
    }

    private string BuildZipEntryName(SubmissionFile file)
    {
        string folder = file.FileKind == SubmissionFileKind.Presentation ? "VideoSunumlari" : "TamMetinler";
        string submissionNumber = string.IsNullOrWhiteSpace(file.Submission.SubmissionNumber)
            ? file.SubmissionId.ToString("N")[..8].ToUpperInvariant()
            : file.Submission.SubmissionNumber.Trim();

        string title = BuildSafeFileNameSegment(ResolveTitle(file.Submission));
        string extension = Path.GetExtension(file.OriginalFileName);
        string fileName = $"{BuildSafeFileNameSegment(submissionNumber)}_{title}{extension}";

        return $"{folder}/{fileName}";
    }

    private string ResolveTitle(Submission submission)
    {
        string culture = CultureInfo.CurrentUICulture.Name;
        bool english = culture.StartsWith("en", StringComparison.OrdinalIgnoreCase);

        if (english && !string.IsNullOrWhiteSpace(submission.TitleEn))
            return submission.TitleEn.Trim();

        return string.IsNullOrWhiteSpace(submission.Title) ? "-" : submission.Title.Trim();
    }

    private static Guid? ParseNullableGuid(string? value)
    {
        return Guid.TryParse(value, out Guid parsed) ? parsed : null;
    }

    private string ResolveCongressName(Symplify.BackOffice.Domain.Congress.Congress congress)
    {
        string culture = CultureInfo.CurrentUICulture.Name;
        string? translated = congress.Translations
            .Where(translation => translation.DeletedDate == null && string.Equals(translation.Language.Culture, culture, StringComparison.OrdinalIgnoreCase))
            .Select(translation => translation.Title)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(translated))
            return translated.Trim();

        return string.IsNullOrWhiteSpace(congress.Name) ? "-" : congress.Name.Trim();
    }

    private string ResolveCongressName(Submission submission)
    {
        string culture = CultureInfo.CurrentUICulture.Name;
        string? translated = submission.Congress.Translations
            .Where(translation => translation.DeletedDate == null && string.Equals(translation.Language.Culture, culture, StringComparison.OrdinalIgnoreCase))
            .Select(translation => translation.Title)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(translated))
            return translated.Trim();

        return string.IsNullOrWhiteSpace(submission.Congress.Name) ? "-" : submission.Congress.Name.Trim();
    }

    private string ResolveSubmissionTypeName(Submission submission)
    {
        if (submission.SubmissionType is null)
            return "-";

        string culture = CultureInfo.CurrentUICulture.Name;
        string? translated = submission.SubmissionType.Translations
            .Where(translation => translation.DeletedDate == null && string.Equals(translation.Language.Culture, culture, StringComparison.OrdinalIgnoreCase))
            .Select(translation => translation.Name)
            .FirstOrDefault();

        return !string.IsNullOrWhiteSpace(translated)
            ? translated.Trim()
            : submission.SubmissionType.Code ?? "-";
    }

    private string ResolveCorrespondingAuthorName(Submission submission)
    {
        Author? author = submission.Authors.FirstOrDefault(item => item.IsCorrespondingAuthor) ?? submission.Authors.FirstOrDefault();
        if (author is null)
            return "-";

        string title = ResolveAuthorTitle(author);
        string fullName = $"{author.FirstName} {author.LastName}".Trim();
        return string.IsNullOrWhiteSpace(title) ? fullName : $"{title} {fullName}".Trim();
    }

    private string ResolveOtherAuthorsText(Submission submission)
    {
        int otherCount = Math.Max(0, submission.Authors.Count - 1);
        return otherCount <= 0
            ? string.Empty
            : string.Format(T("BackOffice.Submissions.Management.Editor.Author.MoreFormat", "+ {0} yazar daha"), otherCount);
    }

    private string ResolveAuthorTitle(Author author)
    {
        if (author.Title is null)
            return string.Empty;

        string culture = CultureInfo.CurrentUICulture.Name;
        string? translated = author.Title.Translations
            .Where(translation => translation.DeletedDate == null && string.Equals(translation.Language.Culture, culture, StringComparison.OrdinalIgnoreCase))
            .Select(translation => !string.IsNullOrWhiteSpace(translation.Description) ? translation.Description : translation.Name)
            .FirstOrDefault();

        return !string.IsNullOrWhiteSpace(translated)
            ? translated.Trim()
            : author.Title.Code ?? string.Empty;
    }

    private string ResolveReviewStatusText(SubmissionFileReviewStatus status)
    {
        return status switch
        {
            SubmissionFileReviewStatus.Approved => T("BackOffice.Submissions.FinalFiles.Status.Approved", "Onaylandı"),
            SubmissionFileReviewStatus.Rejected => T("BackOffice.Submissions.FinalFiles.Status.Rejected", "Reddedildi"),
            SubmissionFileReviewStatus.RevisionRequested => T("BackOffice.Submissions.FinalFiles.Status.RevisionRequested", "Revizyon İstendi"),
            _ => T("BackOffice.Submissions.FinalFiles.Status.PendingReview", "Onay Bekliyor")
        };
    }

    private static string ResolveReviewStatusBadgeClass(SubmissionFileReviewStatus status)
    {
        return status switch
        {
            SubmissionFileReviewStatus.Approved => "bg-success-focus text-success-main",
            SubmissionFileReviewStatus.Rejected => "bg-danger-focus text-danger-main",
            SubmissionFileReviewStatus.RevisionRequested => "bg-info-focus text-info-main",
            _ => "bg-warning-focus text-warning-main"
        };
    }

    private static bool IsAllowedReviewStatus(SubmissionFileReviewStatus status)
    {
        return status is SubmissionFileReviewStatus.PendingReview or SubmissionFileReviewStatus.Approved or SubmissionFileReviewStatus.Rejected or SubmissionFileReviewStatus.RevisionRequested;
    }

    private static bool IsSupportedFinalFileKind(SubmissionFileKind kind)
        => kind is SubmissionFileKind.FullText or SubmissionFileKind.Presentation;

    private bool CanCurrentUserManageFinalFiles()
    {
        return User.IsInRole("Admin") ||
               User.IsInRole("SuperAdmin") ||
               User.IsInRole("Editor") ||
               User.IsInRole("CongressEditor") ||
               User.IsInRole("OrganizationAdmin") ||
               User.Claims.Any(claim =>
                   claim.Type.Contains("Permission", StringComparison.OrdinalIgnoreCase) &&
                   (claim.Value.Contains("Submission", StringComparison.OrdinalIgnoreCase) ||
                    claim.Value.Contains("Editorial", StringComparison.OrdinalIgnoreCase) ||
                    claim.Value.Contains("Management", StringComparison.OrdinalIgnoreCase)));
    }

    private Guid? GetCurrentUserId()
    {
        string? rawId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(rawId, out Guid userId) ? userId : null;
    }

    private string? ResolveSubmissionBucketName()
        => string.IsNullOrWhiteSpace(_objectStorageOptions.Buckets.Submissions)
            ? null
            : _objectStorageOptions.Buckets.Submissions.Trim();

    private static async Task<(byte[]? Content, string? ContentType)> ReadFullTextBookCoverAsync(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0)
            return (null, null);

        if (file.Length > MaxFullTextBookCoverSizeBytes)
        {
            throw new InvalidOperationException(
                "Kapak görseli en fazla 8 MB olabilir.");
        }

        await using MemoryStream stream = new();
        await file.CopyToAsync(stream, cancellationToken);
        byte[] bytes = stream.ToArray();
        string? contentType = DetectFullTextBookCoverContentType(bytes);

        if (contentType is null)
        {
            throw new InvalidOperationException(
                "Kapak görseli geçerli bir PNG veya JPG dosyası olmalıdır.");
        }

        return (bytes, contentType);
    }

    private static string? DetectFullTextBookCoverContentType(ReadOnlySpan<byte> bytes)
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

    private string T(string key, string fallback)
    {
        string value = _localizer.GetStringValue(key);
        return string.IsNullOrWhiteSpace(value) || string.Equals(value, key, StringComparison.OrdinalIgnoreCase)
            ? fallback
            : value;
    }

    private static string NormalizeSortColumn(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? "uploadedat"
            : value.Trim().ToLowerInvariant().Replace("_", string.Empty).Replace("-", string.Empty);

    private static string ResolveFileExtension(string fileName)
    {
        string extension = Path.GetExtension(fileName);
        return string.IsNullOrWhiteSpace(extension)
            ? "-"
            : extension.TrimStart('.').ToUpperInvariant();
    }

    private static string FormatFileSize(long? size)
    {
        if (!size.HasValue || size.Value <= 0)
            return "-";

        string[] units = { "B", "KB", "MB", "GB" };
        double value = size.Value;
        int unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return $"{value:0.#} {units[unitIndex]}";
    }

    private static string BuildSafeFileNameSegment(string? value)
    {
        string normalized = string.IsNullOrWhiteSpace(value) ? "FILE" : value.Trim();
        normalized = normalized
            .Replace("ı", "i", StringComparison.Ordinal)
            .Replace("İ", "I", StringComparison.Ordinal)
            .Replace("ö", "o", StringComparison.Ordinal)
            .Replace("Ö", "O", StringComparison.Ordinal)
            .Replace("ü", "u", StringComparison.Ordinal)
            .Replace("Ü", "U", StringComparison.Ordinal)
            .Replace("ş", "s", StringComparison.Ordinal)
            .Replace("Ş", "S", StringComparison.Ordinal)
            .Replace("ğ", "g", StringComparison.Ordinal)
            .Replace("Ğ", "G", StringComparison.Ordinal)
            .Replace("ç", "c", StringComparison.Ordinal)
            .Replace("Ç", "C", StringComparison.Ordinal);

        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"[^A-Za-z0-9]+", "-");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"-+", "-").Trim('-');

        if (normalized.Length > 80)
            normalized = normalized[..80].Trim('-');

        return string.IsNullOrWhiteSpace(normalized) ? "FILE" : normalized;
    }
}
