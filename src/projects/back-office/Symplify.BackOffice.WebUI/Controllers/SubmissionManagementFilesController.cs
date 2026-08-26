using System.Security.Claims;
using Core.Application.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture?}/submission-management")]
public sealed class SubmissionManagementFilesController : Controller
{
    private readonly ISubmissionFileRepository _submissionFileRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ObjectStorageOptions _objectStorageOptions;

    public SubmissionManagementFilesController(
        ISubmissionFileRepository submissionFileRepository,
        IObjectStorageService objectStorageService,
        IOptions<ObjectStorageOptions> objectStorageOptions)
    {
        _submissionFileRepository = submissionFileRepository;
        _objectStorageService = objectStorageService;
        _objectStorageOptions = objectStorageOptions.Value;
    }

    [HttpGet("files/{fileId:guid}/download")]
    public async Task<IActionResult> DownloadFile(Guid fileId, CancellationToken cancellationToken)
    {
        if (fileId == Guid.Empty)
            return BadRequest();

        SubmissionFile? file = await LoadSubmissionFileAsync(fileId, cancellationToken);
        if (file is null)
            return NotFound();

        if (!CanCurrentUserAccessManagementFile(file))
            return Forbid();

        return await CreateSubmissionFileResultAsync(file, cancellationToken);
    }


    [HttpGet("files/{fileId:guid}/preview")]
    public async Task<IActionResult> PreviewFile(Guid fileId, CancellationToken cancellationToken)
    {
        if (fileId == Guid.Empty)
            return BadRequest();

        SubmissionFile? file = await LoadSubmissionFileAsync(fileId, cancellationToken);
        if (file is null)
            return NotFound();

        if (!CanCurrentUserAccessManagementFile(file))
            return Forbid();

        return await CreateSubmissionFileResultAsync(file, cancellationToken, forceInline: true);
    }

    // Eski editör ekranında FilePath doğrudan link olarak verildiği için tarayıcı şu hatalı URL'i üretiyordu:
    // /submission-management/submissions/{submissionNumber}/acceptance-letters/{authorId}/{fileName}
    // Bu route elde kalmış linklerin de kırılmaması için bırakıldı.
    [HttpGet("submissions/{submissionNumber}/acceptance-letters/{authorSegment}/{fileName}")]
    public async Task<IActionResult> DownloadAcceptanceLetterByLegacyPath(
        string submissionNumber,
        string authorSegment,
        string fileName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(submissionNumber) ||
            string.IsNullOrWhiteSpace(authorSegment) ||
            string.IsNullOrWhiteSpace(fileName))
            return NotFound();

        string objectName = string.Join('/',
            "submissions",
            submissionNumber.Trim(),
            "acceptance-letters",
            authorSegment.Trim(),
            fileName.Trim());

        SubmissionFile? file = await _submissionFileRepository
            .Query()
            .AsNoTracking()
            .Include(item => item.Submission)
                .ThenInclude(submission => submission.Authors)
            .FirstOrDefaultAsync(item =>
                    item.DeletedDate == null &&
                    item.FileKind == SubmissionFileKind.AcceptanceLetter &&
                    item.FilePath == objectName,
                cancellationToken);

        if (file is null)
            return NotFound();

        if (!CanCurrentUserAccessManagementFile(file))
            return Forbid();

        return await CreateSubmissionFileResultAsync(file, cancellationToken);
    }

    private async Task<SubmissionFile?> LoadSubmissionFileAsync(Guid fileId, CancellationToken cancellationToken)
    {
        return await _submissionFileRepository
            .Query()
            .AsNoTracking()
            .Include(item => item.Submission)
                .ThenInclude(submission => submission.Authors)
            .FirstOrDefaultAsync(item => item.Id == fileId && item.DeletedDate == null, cancellationToken);
    }

    private async Task<IActionResult> CreateSubmissionFileResultAsync(
        SubmissionFile file,
        CancellationToken cancellationToken,
        bool forceInline = false)
    {
        if (string.IsNullOrWhiteSpace(file.FilePath))
            return NotFound();

        string objectName = file.FilePath.Trim();

        if (objectName.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            objectName.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return Redirect(objectName);

        string? bucketName = ResolveSubmissionBucketName();
        if (string.IsNullOrWhiteSpace(bucketName))
            return Problem("Submission file storage bucket is not configured.");

        Stream stream = await _objectStorageService.OpenReadAsync(bucketName, objectName, cancellationToken);

        string downloadName = string.IsNullOrWhiteSpace(file.OriginalFileName)
            ? Path.GetFileName(objectName)
            : file.OriginalFileName;

        string contentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType;

        if (forceInline)
        {
            Response.Headers[HeaderNames.ContentDisposition] = $"inline; filename*=UTF-8''{Uri.EscapeDataString(downloadName)}";
            return File(stream, contentType, enableRangeProcessing: true);
        }

        return File(stream, contentType, downloadName, enableRangeProcessing: true);
    }

    private string? ResolveSubmissionBucketName()
        => string.IsNullOrWhiteSpace(_objectStorageOptions.Buckets.Submissions)
            ? null
            : _objectStorageOptions.Buckets.Submissions.Trim();

    private bool CanCurrentUserAccessManagementFile(SubmissionFile file)
    {
        if (User.Identity?.IsAuthenticated != true)
            return false;

        if (User.IsInRole("Admin") ||
            User.IsInRole("SuperAdmin") ||
            User.IsInRole("Editor") ||
            User.IsInRole("CongressEditor") ||
            User.IsInRole("OrganizationAdmin"))
            return true;

        if (User.Claims.Any(claim =>
                claim.Type.Contains("Permission", StringComparison.OrdinalIgnoreCase) &&
                (claim.Value.Contains("Submission", StringComparison.OrdinalIgnoreCase) ||
                 claim.Value.Contains("Editorial", StringComparison.OrdinalIgnoreCase) ||
                 claim.Value.Contains("Management", StringComparison.OrdinalIgnoreCase))))
            return true;

        Guid? currentUserId = GetCurrentUserId();
        if (currentUserId.HasValue && file.Submission.CreatedByUserId == currentUserId.Value)
            return true;

        string? currentEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(currentEmail) &&
            file.Submission.Authors.Any(author =>
                !string.IsNullOrWhiteSpace(author.Email) &&
                string.Equals(author.Email.Trim(), currentEmail.Trim(), StringComparison.OrdinalIgnoreCase)))
            return true;

        return false;
    }

    private Guid? GetCurrentUserId()
    {
        string? rawId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(rawId, out Guid userId) ? userId : null;
    }
}
