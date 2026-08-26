using System.Security.Claims;
using Core.Application.Requests;
using Core.Application.Storage;
using ApplicationValidationException = Core.CrossCuttingConcerns.Exceptions.Types.ValidationException;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Features.Submissions.Commands.Create;
using Symplify.BackOffice.Application.Common.Text;
using Symplify.BackOffice.Application.Features.Submissions.Commands.Delete;
using Symplify.BackOffice.Application.Features.Submissions.Commands.Update;
using Symplify.BackOffice.Application.Common.Storage;
using Symplify.BackOffice.Application.Features.Submissions.Queries.GetById;
using Symplify.BackOffice.Application.Features.Submissions.Queries.GetCreatePage;
using Symplify.BackOffice.Application.Features.Submissions.Queries.GetList;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Identity;
using Symplify.BackOffice.Domain.Submission;
using Symplify.BackOffice.Domain.Workflow;
using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.WebUI.Models.Submissions;
using Symplify.BackOffice.WebUI.Models.Shared.DataTables;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture?}/submissions")]
public sealed class SubmissionsController : Controller
{
    private const int DefaultPageIndex = 0;
    private const int DefaultPageSize = 50;
    private const long MaxFullTextFileSize = 50 * 1024 * 1024;
    private const long MaxPresentationVideoFileSize = 500L * 1024 * 1024;

    private static readonly HashSet<string> AllowedFullTextFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".docx"
    };

    private static readonly HashSet<string> AllowedPresentationVideoFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4",
        ".mov",
        ".webm"
    };

    private readonly IMediator _mediator;
    private readonly UserManager<AppUser> _userManager;
    private readonly ISubmissionRepository _submissionRepository;
    private readonly ISubmissionFileRepository _submissionFileRepository;
    private readonly ISubmissionAcceptanceLetterRepository _acceptanceLetterRepository;
    private readonly IPaymentDocumentRepository _paymentDocumentRepository;
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly ICongressRepository _congressRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly IBackOfficeViewLocalizer _localizer;
    private readonly ObjectStorageOptions _objectStorageOptions;

    public SubmissionsController(
        IMediator mediator,
        UserManager<AppUser> userManager,
        ISubmissionRepository submissionRepository,
        ISubmissionFileRepository submissionFileRepository,
        ISubmissionAcceptanceLetterRepository acceptanceLetterRepository,
        IPaymentDocumentRepository paymentDocumentRepository,
        IOrganizationUserRepository organizationUserRepository,
        ICongressRepository congressRepository,
        IObjectStorageService objectStorageService,
        IBackOfficeViewLocalizer localizer,
        IOptions<ObjectStorageOptions> objectStorageOptions)
    {
        _mediator = mediator;
        _userManager = userManager;
        _submissionRepository = submissionRepository;
        _submissionFileRepository = submissionFileRepository;
        _acceptanceLetterRepository = acceptanceLetterRepository;
        _paymentDocumentRepository = paymentDocumentRepository;
        _organizationUserRepository = organizationUserRepository;
        _congressRepository = congressRepository;
        _objectStorageService = objectStorageService;
        _localizer = localizer;
        _objectStorageOptions = objectStorageOptions.Value;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken,
        int page = DefaultPageIndex,
        int pageSize = DefaultPageSize,
        string? searchText = null)
    {
        var response = await _mediator.Send(new GetListSubmissionQuery
        {
            PageRequest = new PageRequest
            {
                Page = page,
                PageSize = pageSize
            },
            CreatedByUserId = GetCurrentUserId(),
            Culture = RouteData.Values["culture"]?.ToString(),
            SearchText = searchText
        }, cancellationToken);

        ViewData["SearchText"] = searchText;
        return View(response);
    }

    [HttpPost("get-list")]
    [HttpPost("GetList")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GetList(
        [FromForm] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        DataTableQueryOptions tableOptions = DataTableQueryOptions.From(
            request,
            defaultSortColumn: "submittedAt",
            defaultSortDirection: "desc",
            allowedSortColumns: new[] { "submittedAt", "title", "submissionNumber", "topic", "status" });

        var response = await _mediator.Send(new GetListSubmissionQuery
        {
            PageRequest = new PageRequest
            {
                Page = tableOptions.Page,
                PageSize = tableOptions.PageSize
            },
            CreatedByUserId = GetCurrentUserId(),
            Culture = RouteData.Values["culture"]?.ToString(),
            SearchText = tableOptions.SearchText
        }, cancellationToken);

        List<object> pageItems = response.Items
            .Select((item, index) => ToDataTableRow(item, tableOptions.Start + index + 1))
            .Cast<object>()
            .ToList();

        return Json(new
        {
            draw = request.Draw,
            recordsTotal = response.Count,
            recordsFiltered = response.Count,
            data = pageItems
        });
    }

    [HttpGet("details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest();

        Submission? submission = await LoadSubmissionForAuthorActionAsync(id, cancellationToken);
        if (submission is null)
            return NotFound();

        if (!CanCurrentUserAccessSubmission(submission))
            return Forbid();

        GetByIdSubmissionResponse response = await _mediator.Send(new GetByIdSubmissionQuery
        {
            Id = id,
            Culture = RouteData.Values["culture"]?.ToString()
        }, cancellationToken);

        return View(response);
    }

    [HttpPost("details/{id:guid}/payment-document")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> UploadPaymentDocument(Guid id, IFormFile? paymentDocument, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest();

        Submission? submission = await LoadSubmissionForAuthorActionAsync(id, cancellationToken);
        if (submission is null)
            return NotFound();

        if (!CanCurrentUserAccessSubmission(submission))
            return Forbid();

        if (!CanUploadPaymentDocumentForSubmission(submission))
        {
            TempData["ErrorMessage"] = "Bu bildiri için şu anda ödeme belgesi yüklenemez.";
            return RedirectToAction(nameof(Details), new { culture = RouteData.Values["culture"]?.ToString(), id });
        }

        if (paymentDocument is null || paymentDocument.Length <= 0)
        {
            TempData["ErrorMessage"] = "Lütfen ödeme belgesi seçin.";
            return RedirectToAction(nameof(Details), new { culture = RouteData.Values["culture"]?.ToString(), id });
        }

        string extension = Path.GetExtension(paymentDocument.FileName);
        if (!IsAllowedPaymentDocumentExtension(extension))
        {
            TempData["ErrorMessage"] = "Ödeme belgesi PDF, JPG, PNG veya WEBP formatında olmalıdır.";
            return RedirectToAction(nameof(Details), new { culture = RouteData.Values["culture"]?.ToString(), id });
        }

        const long maxPaymentDocumentSize = 20 * 1024 * 1024;
        if (paymentDocument.Length > maxPaymentDocumentSize)
        {
            TempData["ErrorMessage"] = "Ödeme belgesi en fazla 20 MB olabilir.";
            return RedirectToAction(nameof(Details), new { culture = RouteData.Values["culture"]?.ToString(), id });
        }

        string? bucketName = ResolveSubmissionBucketName();
        if (string.IsNullOrWhiteSpace(bucketName))
            return Problem("Submission file storage bucket is not configured.");

        string safeOriginalFileName = string.IsNullOrWhiteSpace(paymentDocument.FileName)
            ? $"payment-document{extension}"
            : Path.GetFileName(paymentDocument.FileName);

        string objectName = BackOfficeObjectStorageHelper.BuildObjectName(
            "submissions",
            ResolveSubmissionStorageSegment(submission),
            "payment-documents",
            $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}");

        await using Stream content = paymentDocument.OpenReadStream();
        ObjectStorageUploadResult uploadResult = await _objectStorageService.UploadAsync(
            new ObjectStorageUploadRequest
            {
                BucketName = bucketName,
                ObjectName = objectName,
                OriginalFileName = safeOriginalFileName,
                ContentType = BackOfficeObjectStorageHelper.NormalizeContentType(paymentDocument.ContentType),
                Size = paymentDocument.Length,
                Content = content,
                Metadata = new Dictionary<string, string>
                {
                    ["module"] = "submission-payment-document",
                    ["submission-id"] = submission.Id.ToString("D"),
                    ["congress-id"] = submission.CongressId.ToString("D")
                }
            },
            cancellationToken);

        PaymentDocument document = new()
        {
            Id = Guid.NewGuid(),
            SubmissionId = submission.Id,
            CongressId = submission.CongressId,
            FilePath = uploadResult.ObjectName,
            OriginalFileName = safeOriginalFileName,
            ContentType = uploadResult.ContentType,
            Size = uploadResult.Size,
            IsApproved = false,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = GetCurrentUserId()?.ToString()
        };

        await _paymentDocumentRepository.AddAsync(document);

        TempData["SuccessMessage"] = "Ödeme belgesi yüklendi. Belgeniz yönetici tarafından kontrol edilecektir.";
        return RedirectToAction(nameof(Details), new { culture = RouteData.Values["culture"]?.ToString(), id });
    }

    [HttpPost("details/{id:guid}/full-text")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(50 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 50 * 1024 * 1024)]
    public async Task<IActionResult> UploadFullTextFile(Guid id, IFormFile? fullTextFile, CancellationToken cancellationToken)
    {
        return await UploadFinalSubmissionFileAsync(
            id,
            fullTextFile,
            SubmissionFileKind.FullText,
            "full-text",
            MaxFullTextFileSize,
            AllowedFullTextFileExtensions,
            T("BackOffice.Submissions.FinalFiles.Validation.FullTextRequired", "Lütfen tam metin dosyası seçin."),
            T("BackOffice.Submissions.FinalFiles.Validation.FullTextType", "Tam metin dosyası yalnızca DOCX formatında olmalıdır."),
            T("BackOffice.Submissions.FinalFiles.Validation.FullTextSize", "Tam metin dosyası en fazla 50 MB olabilir."),
            T("BackOffice.Submissions.FinalFiles.UploadFullTextSuccess", "Tam metin dosyanız yüklendi."),
            cancellationToken);
    }

    [HttpPost("details/{id:guid}/presentation-video")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(500L * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 500L * 1024 * 1024)]
    public async Task<IActionResult> UploadPresentationVideoFile(Guid id, IFormFile? presentationFile, CancellationToken cancellationToken)
    {
        return await UploadFinalSubmissionFileAsync(
            id,
            presentationFile,
            SubmissionFileKind.Presentation,
            "presentation-video",
            MaxPresentationVideoFileSize,
            AllowedPresentationVideoFileExtensions,
            T("BackOffice.Submissions.FinalFiles.Validation.VideoRequired", "Lütfen video sunum dosyası seçin."),
            T("BackOffice.Submissions.FinalFiles.Validation.VideoType", "Video sunum dosyası MP4, MOV veya WEBM formatında olmalıdır."),
            T("BackOffice.Submissions.FinalFiles.Validation.VideoSize", "Video sunum dosyası en fazla 500 MB olabilir."),
            T("BackOffice.Submissions.FinalFiles.UploadVideoSuccess", "Video sunum dosyanız yüklendi."),
            cancellationToken);
    }

    [HttpGet("payment-documents/{documentId:guid}/download")]
    public async Task<IActionResult> DownloadPaymentDocument(Guid documentId, CancellationToken cancellationToken)
    {
        if (documentId == Guid.Empty)
            return BadRequest();

        PaymentDocument? document = await _paymentDocumentRepository
            .Query()
            .AsNoTracking()
            .Include(item => item.Submission)
                .ThenInclude(submission => submission!.Authors)
            .Include(item => item.Submission)
                .ThenInclude(submission => submission!.TransactionStatus)
            .FirstOrDefaultAsync(item => item.Id == documentId && item.DeletedDate == null, cancellationToken);

        if (document is null || document.Submission is null)
            return NotFound();

        if (!CanCurrentUserAccessSubmission(document.Submission))
            return Forbid();

        if (IsRejectedSubmission(document.Submission) && !CanCurrentUserManageSubmissions())
            return Forbid();

        if (string.IsNullOrWhiteSpace(document.FilePath))
            return NotFound();

        string? bucketName = ResolveSubmissionBucketName();
        if (string.IsNullOrWhiteSpace(bucketName))
            return Problem("Submission file storage bucket is not configured.");

        Stream stream = await _objectStorageService.OpenReadAsync(bucketName, document.FilePath.Trim(), cancellationToken);
        string downloadName = string.IsNullOrWhiteSpace(document.OriginalFileName)
            ? Path.GetFileName(document.FilePath)
            : document.OriginalFileName!;
        string contentType = string.IsNullOrWhiteSpace(document.ContentType)
            ? "application/octet-stream"
            : document.ContentType!;

        return File(stream, contentType, downloadName, enableRangeProcessing: true);
    }

    [HttpGet("files/{fileId:guid}/download")]
    public async Task<IActionResult> DownloadFile(Guid fileId, CancellationToken cancellationToken)
    {
        if (fileId == Guid.Empty)
            return BadRequest();

        SubmissionFile? file = await LoadSubmissionFileAsync(fileId, cancellationToken);
        if (file is null)
            return NotFound();

        if (!CanCurrentUserAccessSubmissionFile(file))
            return Forbid();

        if (file.Submission is not null && IsRejectedSubmission(file.Submission) && !CanCurrentUserManageSubmissions())
            return Forbid();

        return await CreateSubmissionFileResultAsync(file, cancellationToken);
    }


    [HttpGet("acceptance-letters/{letterId:guid}/download")]
    public async Task<IActionResult> DownloadAcceptanceLetter(Guid letterId, CancellationToken cancellationToken)
    {
        if (letterId == Guid.Empty)
            return BadRequest();

        SubmissionAcceptanceLetter? letter = await _acceptanceLetterRepository
            .Query()
            .AsNoTracking()
            .Include(item => item.Submission)
                .ThenInclude(submission => submission.Authors)
            .Include(item => item.Submission)
                .ThenInclude(submission => submission.TransactionStatus)
            .FirstOrDefaultAsync(item => item.Id == letterId && item.DeletedDate == null, cancellationToken);

        if (letter is null)
            return NotFound();

        if (!CanCurrentUserAccessSubmission(letter.Submission))
            return Forbid();

        if (IsRejectedSubmission(letter.Submission) && !CanCurrentUserManageSubmissions())
            return Forbid();

        string? objectName = letter.PdfObjectName ?? letter.PdfFilePath;
        if (string.IsNullOrWhiteSpace(objectName))
            return NotFound();

        string? bucketName = ResolveSubmissionBucketName();
        if (string.IsNullOrWhiteSpace(bucketName))
            return Problem("Submission file storage bucket is not configured.");

        Stream stream = await _objectStorageService.OpenReadAsync(bucketName, objectName.Trim(), cancellationToken);
        string downloadName = string.IsNullOrWhiteSpace(letter.FileName)
            ? Path.GetFileName(objectName)
            : letter.FileName;
        string contentType = string.IsNullOrWhiteSpace(letter.PdfContentType)
            ? "application/pdf"
            : letter.PdfContentType!;

        return File(stream, contentType, downloadName, enableRangeProcessing: true);
    }

    // Eski ekranda FilePath doğrudan href olarak kullanıldığı için tarayıcı bu hatalı göreli URL'yi üretiyordu:
    // /submissions/details/submissions/{submissionNumber}/acceptance-letters/{authorId}/{fileName}
    // Yeni ekran DownloadFile action'ını kullanır; bu route sadece eski/elde kalmış linklerin kırılmaması içindir.
    [HttpGet("details/submissions/{submissionNumber}/acceptance-letters/{authorSegment}/{fileName}")]
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
            .Include(item => item.Submission)
                .ThenInclude(submission => submission.TransactionStatus)
            .FirstOrDefaultAsync(item =>
                    item.DeletedDate == null &&
                    item.FileKind == SubmissionFileKind.AcceptanceLetter &&
                    item.FilePath == objectName,
                cancellationToken);

        if (file is null)
            return NotFound();

        if (!CanCurrentUserAccessSubmissionFile(file))
            return Forbid();

        if (file.Submission is not null && IsRejectedSubmission(file.Submission) && !CanCurrentUserManageSubmissions())
            return Forbid();

        return await CreateSubmissionFileResultAsync(file, cancellationToken);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(Guid? submissionTypeId, CancellationToken cancellationToken)
    {
        SubmissionCreateViewModel model = await BuildCreateViewModelAsync(
            new SubmissionCreateViewModel
            {
                SubmissionTypeId = NormalizeOptionalGuid(submissionTypeId)
            },
            cancellationToken);

        if (model.SubmissionTypeId.HasValue &&
            model.SelectedSubmissionTypeFormProfile == SubmissionFormProfile.ExhibitionApplication)
        {
            return RedirectToAction(
                "Create",
                "ExhibitionApplications",
                new
                {
                    culture = RouteData.Values["culture"]?.ToString(),
                    submissionTypeId = model.SubmissionTypeId.Value
                });
        }

        return View(model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> Create(SubmissionCreateViewModel model, CancellationToken cancellationToken)
    {
        model.Authors = NormalizePostedAuthors(model.Authors);

        SubmissionCongressContext? submissionCongress = await ResolveCurrentUserSubmissionCongressAsync(cancellationToken);
        ModelState.Remove(nameof(model.CongressId));

        if (submissionCongress is null)
        {
            model.CongressId = Guid.Empty;
            model.CongressName = string.Empty;
            ModelState.AddModelError(nameof(model.CongressId), T("BackOffice.Submissions.Create.Validation.NoActiveCongress", "Bildiri gönderebileceğiniz aktif bir kongre üyeliği bulunamadı."));
        }
        else
        {
            model.CongressId = submissionCongress.Id;
            model.CongressName = submissionCongress.Name;
        }

        if (!model.Authors.Any(author => author.IsCorrespondingAuthor))
            ModelState.AddModelError(nameof(model.Authors), T("BackOffice.Submissions.AuthorList.Validation.AtLeastOneCorrespondingAuthor", "En az bir sorumlu yazar eklenmelidir."));

        if (model.Authors.Any(author => !author.TitleId.HasValue || author.TitleId.Value == Guid.Empty))
            ModelState.AddModelError(nameof(model.Authors), T("BackOffice.Submissions.AuthorList.Validation.TitleRequired", "Listedeki her yazar için unvan seçilmelidir."));

        if (model.Authors.Any(author => string.IsNullOrWhiteSpace(author.Institution)))
            ModelState.AddModelError(nameof(model.Authors), T("BackOffice.Submissions.AuthorList.Validation.InstitutionRequired", "Listedeki her yazar için kurum bilgisi girilmelidir."));

        LocalizeSubmissionCreateModelState(model);

        model = await BuildCreateViewModelAsync(model, cancellationToken);

        if (model.SubmissionTypeId.HasValue &&
            model.SelectedSubmissionTypeFormProfile == SubmissionFormProfile.ExhibitionApplication)
        {
            ModelState.AddModelError(
                nameof(model.SubmissionTypeId),
                T("BackOffice.Submissions.Create.Validation.ExhibitionFormOnly", "Sergi başvuruları sergi başvuru formundan gönderilmelidir."));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            CreatedSubmissionResponse response = await _mediator.Send(new CreateSubmissionCommand
            {
                CongressId = model.CongressId,
                SubmissionTypeId = model.SubmissionTypeId,
                TopicId = model.TopicId,
                LanguageId = model.LanguageId,
                CreatedByUserId = GetCurrentUserId(),
                Orcid = model.Orcid,
                Title = model.Title,
                TitleEn = model.TitleEn,
                Abstract = model.Abstract,
                AbstractEn = model.AbstractEn,
                Keywords = model.Keywords,
                KeywordsEn = model.KeywordsEn,
                SubmitForReview = string.Equals(model.SubmitAction, "submit", StringComparison.OrdinalIgnoreCase),
                Authors = model.Authors.Select(author => new SubmissionAuthorInputDto
                {
                    TitleId = author.TitleId,
                    FirstName = author.FirstName,
                    LastName = author.LastName,
                    FullName = author.FullName,
                    Email = author.Email,
                    Institution = author.Institution,
                    Orcid = author.Orcid,
                    IsCorrespondingAuthor = author.IsCorrespondingAuthor
                }).ToList()
            }, cancellationToken);

            TempData["SuccessMessage"] = response.IsSubmitted
                ? "Bildiri oluşturuldu ve onaya gönderildi."
                : "Bildiri taslak olarak kaydedildi.";

            return RedirectToIndex();
        }
        catch (ApplicationValidationException exception)
        {
            AddApplicationValidationErrorsToModelState(exception);
            model = await BuildCreateViewModelAsync(model, cancellationToken);
            return View(model);
        }
    }

    [HttpGet("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest();

        Submission? submission = await LoadSubmissionForAuthorActionAsync(id, cancellationToken);
        if (submission is null)
            return NotFound();

        if (!CanCurrentUserAccessSubmission(submission))
            return Forbid();

        if (IsRejectedSubmission(submission) && !CanCurrentUserManageSubmissions())
        {
            TempData["ErrorMessage"] = "Bu bildiri reddedildiği için düzenlenemez.";
            return RedirectToAction(nameof(Details), new { culture = RouteData.Values["culture"]?.ToString(), id });
        }

        SubmissionUpdateViewModel model = await BuildUpdateViewModelAsync(id, null, cancellationToken);
        return View(model);
    }

    [HttpPost("edit/{id:guid}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> Edit(Guid id, SubmissionUpdateViewModel model, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty || model.Id == Guid.Empty || id != model.Id)
            return BadRequest();

        Submission? existingSubmission = await LoadSubmissionForAuthorActionAsync(id, cancellationToken);
        if (existingSubmission is null)
            return NotFound();

        if (!CanCurrentUserAccessSubmission(existingSubmission))
            return Forbid();

        if (IsRejectedSubmission(existingSubmission) && !CanCurrentUserManageSubmissions())
        {
            TempData["ErrorMessage"] = "Bu bildiri reddedildiği için düzenlenemez.";
            return RedirectToAction(nameof(Details), new { culture = RouteData.Values["culture"]?.ToString(), id });
        }

        model.Authors = NormalizePostedAuthors(model.Authors);

        if (!model.Authors.Any(author => author.IsCorrespondingAuthor))
            ModelState.AddModelError(nameof(model.Authors), T("BackOffice.Submissions.AuthorList.Validation.AtLeastOneCorrespondingAuthor", "En az bir sorumlu yazar eklenmelidir."));

        if (model.Authors.Any(author => !author.TitleId.HasValue || author.TitleId.Value == Guid.Empty))
            ModelState.AddModelError(nameof(model.Authors), T("BackOffice.Submissions.AuthorList.Validation.TitleRequired", "Listedeki her yazar için unvan seçilmelidir."));

        if (model.Authors.Any(author => string.IsNullOrWhiteSpace(author.Institution)))
            ModelState.AddModelError(nameof(model.Authors), T("BackOffice.Submissions.AuthorList.Validation.InstitutionRequired", "Listedeki her yazar için kurum bilgisi girilmelidir."));

        LocalizeSubmissionUpdateModelState(model);

        if (!ModelState.IsValid)
        {
            model = await BuildUpdateViewModelAsync(id, model, cancellationToken);
            return View(model);
        }

        try
        {
            await _mediator.Send(new UpdateSubmissionCommand
            {
                Id = model.Id,
                SubmissionTypeId = model.SubmissionTypeId,
                TopicId = model.TopicId,
                LanguageId = model.LanguageId,
                RequestedByUserId = GetCurrentUserId(),
                Orcid = model.Orcid,
                Title = model.Title,
                TitleEn = model.TitleEn,
                Abstract = model.Abstract,
                AbstractEn = model.AbstractEn,
                Keywords = model.Keywords,
                KeywordsEn = model.KeywordsEn,
                SubmitForReview = string.Equals(model.SubmitAction, "submit", StringComparison.OrdinalIgnoreCase),
                Authors = model.Authors.Select(author => new SubmissionAuthorInputDto
                {
                    Id = author.Id,
                    TitleId = author.TitleId,
                    FirstName = author.FirstName,
                    LastName = author.LastName,
                    FullName = author.FullName,
                    Email = author.Email,
                    Institution = author.Institution,
                    Orcid = author.Orcid,
                    IsCorrespondingAuthor = author.IsCorrespondingAuthor
                }).ToList()
            }, cancellationToken);

            TempData["SuccessMessage"] = string.Equals(model.SubmitAction, "submit", StringComparison.OrdinalIgnoreCase)
                ? "Bildiri güncellendi ve onaya gönderildi."
                : "Bildiri güncellendi.";

            return RedirectToIndex();
        }
        catch (ApplicationValidationException exception)
        {
            AddApplicationValidationErrorsToModelState(exception);
            model = await BuildUpdateViewModelAsync(id, model, cancellationToken);
            return View(model);
        }
    }

    [HttpPost("delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(DeleteSubmissionCommand command, CancellationToken cancellationToken)
    {
        Submission? submission = await LoadSubmissionForAuthorActionAsync(command.Id, cancellationToken);
        if (submission is null)
            return NotFound();

        if (!CanCurrentUserAccessSubmission(submission))
            return Forbid();

        if (IsRejectedSubmission(submission) && !CanCurrentUserManageSubmissions())
        {
            TempData["ErrorMessage"] = "Bu bildiri reddedildiği için silinemez.";
            return RedirectToIndex();
        }

        command.RequestedByUserId = GetCurrentUserId();
        await _mediator.Send(command, cancellationToken);

        TempData["SuccessMessage"] = "Bildiri silindi.";
        return RedirectToIndex();
    }

    private async Task<SubmissionCreateViewModel> BuildCreateViewModelAsync(
        SubmissionCreateViewModel model,
        CancellationToken cancellationToken)
    {
        SubmissionCongressContext? submissionCongress = await ResolveCurrentUserSubmissionCongressAsync(cancellationToken);

        if (submissionCongress is null)
        {
            model.CongressId = Guid.Empty;
            model.CongressName = string.Empty;
            model.Congresses = Array.Empty<SubmissionCreateSelectItemViewModel>();
            model.SubmissionTypes = Array.Empty<SubmissionCreateSelectItemViewModel>();
            model.Topics = Array.Empty<SubmissionCreateSelectItemViewModel>();
            model.Languages = Array.Empty<SubmissionCreateSelectItemViewModel>();
            model.TitleOptions = Array.Empty<SubmissionCreateSelectItemViewModel>();

            await EnsureCurrentUserAuthorAsync(model);
            return model;
        }

        model.CongressId = submissionCongress.Id;

        GetSubmissionCreatePageResponse response = await GetCreatePageResponseAsync(model.CongressId, cancellationToken);

        model.Congresses = response.Congresses
            .Where(item => item.Id == model.CongressId)
            .Select(MapSelectItem)
            .ToList();
        model.SubmissionTypes = response.SubmissionTypes.Select(MapSelectItem).ToList();
        model.Topics = response.Topics.Select(MapSelectItem).ToList();
        model.Languages = response.Languages.Select(MapSelectItem).ToList();
        model.TitleOptions = response.Titles.Select(MapSelectItem).ToList();
        model.CongressName = response.Congresses.FirstOrDefault(item => item.Id == model.CongressId)?.Text
            ?? submissionCongress.Name;

        ApplySubmissionTypeSelection(model);

        if (!model.LanguageId.HasValue && response.DefaultLanguageId.HasValue)
            model.LanguageId = response.DefaultLanguageId.Value;

        await EnsureCurrentUserAuthorAsync(model);

        return model;
    }

    private async Task<SubmissionUpdateViewModel> BuildUpdateViewModelAsync(
        Guid id,
        SubmissionUpdateViewModel? postedModel,
        CancellationToken cancellationToken)
    {
        GetByIdSubmissionResponse submission = await _mediator.Send(new GetByIdSubmissionQuery
        {
            Id = id
        }, cancellationToken);

        SubmissionUpdateViewModel model = postedModel ?? new SubmissionUpdateViewModel
        {
            Id = submission.Id,
            CongressId = submission.CongressId,
            SubmissionTypeId = submission.SubmissionTypeId,
            TopicId = submission.TopicId,
            LanguageId = submission.LanguageId,
            SubmissionNumber = submission.SubmissionNumber,
            Orcid = submission.Orcid,
            Title = submission.Title,
            TitleEn = submission.TitleEn,
            Abstract = submission.Abstract ?? string.Empty,
            AbstractEn = submission.AbstractEn,
            Keywords = submission.Keywords ?? string.Empty,
            KeywordsEn = submission.KeywordsEn,
            IsSubmitted = submission.IsSubmitted,
            SubmittedAt = submission.SubmittedAt,
            UpdatedDate = submission.UpdatedDate,
            Authors = submission.Authors.Select(author => new SubmissionAuthorInputViewModel
            {
                Id = author.Id,
                TitleId = author.TitleId,
                TitleName = author.TitleName,
                FirstName = BackOfficeTextNormalizer.NormalizePersonFirstName(author.FirstName),
                LastName = BackOfficeTextNormalizer.NormalizePersonSurname(author.LastName),
                FullName = BackOfficeTextNormalizer.NormalizePersonFullName(author.FirstName, author.LastName),
                Email = author.Email,
                Institution = author.Institution,
                Orcid = author.Orcid,
                IsCorrespondingAuthor = author.IsCorrespondingAuthor
            }).ToList()
        };

        model.Id = submission.Id;
        model.CongressId = submission.CongressId;
        model.SubmissionNumber = submission.SubmissionNumber;
        model.IsSubmitted = submission.IsSubmitted;
        model.CanEdit = submission.CanEdit;
        model.TransactionStatusName = submission.TransactionStatusName;
        model.TransactionStatusCode = submission.TransactionStatusCode;
        model.SubmittedAt = submission.SubmittedAt;
        model.UpdatedDate = submission.UpdatedDate;

        GetSubmissionCreatePageResponse createPage = await GetCreatePageResponseAsync(submission.CongressId, cancellationToken);
        model.CongressName = createPage.Congresses.FirstOrDefault(item => item.Id == submission.CongressId)?.Text ?? submission.CongressId.ToString();
        model.SubmissionTypes = createPage.SubmissionTypes.Select(MapSelectItem).ToList();
        model.Topics = createPage.Topics.Select(MapSelectItem).ToList();
        model.Languages = createPage.Languages.Select(MapSelectItem).ToList();
        model.TitleOptions = createPage.Titles.Select(MapSelectItem).ToList();

        return model;
    }

    private async Task<GetSubmissionCreatePageResponse> GetCreatePageResponseAsync(Guid? congressId, CancellationToken cancellationToken)
    {
        return await _mediator.Send(new GetSubmissionCreatePageQuery
        {
            CongressId = congressId,
            Culture = RouteData.Values["culture"]?.ToString()
        }, cancellationToken);
    }

    private static SubmissionCreateSelectItemViewModel MapSelectItem(SubmissionCreateSelectItemDto item)
    {
        return new SubmissionCreateSelectItemViewModel
        {
            Id = item.Id,
            Text = item.Text,
            FormProfile = item.FormProfile
        };
    }

    private static void ApplySubmissionTypeSelection(SubmissionCreateViewModel model)
    {
        model.IsSubmissionTypeLocked = false;
        model.SelectedSubmissionTypeName = null;
        model.SelectedSubmissionTypeFormProfile = SubmissionFormProfile.AcademicAbstract;

        if (!model.SubmissionTypeId.HasValue || model.SubmissionTypeId.Value == Guid.Empty)
            return;

        SubmissionCreateSelectItemViewModel? selected = model.SubmissionTypes
            .FirstOrDefault(item => item.Id == model.SubmissionTypeId.Value);

        if (selected is null)
        {
            model.SubmissionTypeId = null;
            return;
        }

        model.SelectedSubmissionTypeName = selected.Text;
        model.SelectedSubmissionTypeFormProfile = selected.FormProfile;
        model.IsSubmissionTypeLocked = true;
    }

    private async Task<SubmissionCongressContext?> ResolveCurrentUserSubmissionCongressAsync(CancellationToken cancellationToken)
    {
        Guid? currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
            return null;

        Guid? currentOrganizationId = GetCurrentOrganizationId();

        var organizationUserQuery = _organizationUserRepository
            .Query()
            .AsNoTracking()
            .Where(item =>
                item.UserId == currentUserId.Value &&
                item.IsActive &&
                item.DeletedDate == null);

        if (currentOrganizationId.HasValue && currentOrganizationId.Value != Guid.Empty)
            organizationUserQuery = organizationUserQuery.Where(item => item.OrganizationId == currentOrganizationId.Value);

        var organizationUser = await organizationUserQuery
            .OrderByDescending(item => item.CreatedDate)
            .ThenBy(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (organizationUser is null)
            return null;

        var publishedCongress = await _congressRepository
            .Query()
            .AsNoTracking()
            .Where(item =>
                item.OrganizationId == organizationUser.OrganizationId &&
                item.Status == CongressStatus.Published &&
                item.DeletedDate == null)
            .OrderByDescending(item => item.StartDate)
            .ThenByDescending(item => item.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (publishedCongress is null)
            return null;

        string congressName = !string.IsNullOrWhiteSpace(publishedCongress.Name)
            ? publishedCongress.Name
            : publishedCongress.Id.ToString();

        return new SubmissionCongressContext(publishedCongress.Id, congressName);
    }

    private async Task EnsureCurrentUserAuthorAsync(SubmissionCreateViewModel model)
    {
        if (model.Authors.Count > 0)
            return;

        AppUser? currentUser = await _userManager.GetUserAsync(User);
        string? email = currentUser?.Email ?? User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
        string fullName = BuildCurrentUserFullName(currentUser, email);

        if (string.IsNullOrWhiteSpace(fullName))
            return;

        model.Authors.Add(new SubmissionAuthorInputViewModel
        {
            TitleId = currentUser?.TitleId,
            TitleName = null,
            FirstName = BackOfficeTextNormalizer.NormalizePersonFirstName(currentUser?.Name),
            LastName = BackOfficeTextNormalizer.NormalizePersonSurname(currentUser?.Surname),
            FullName = fullName,
            Email = email?.Trim() ?? string.Empty,
            Institution = currentUser?.Institution,
            Orcid = currentUser?.Orcid,
            IsCorrespondingAuthor = true
        });
    }


    private Guid? GetCurrentOrganizationId()
    {
        string? organizationId = User.FindFirstValue("OrganizationId");
        return Guid.TryParse(organizationId, out Guid parsedOrganizationId)
            ? parsedOrganizationId
            : null;
    }

    private static string BuildCurrentUserFullName(AppUser? user, string? fallbackEmail)
    {
        if (user is not null)
        {
            string fullName = string.Join(' ', new[] { user.Name, user.Surname }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim()));

            if (!string.IsNullOrWhiteSpace(fullName))
                return fullName;
        }

        return string.IsNullOrWhiteSpace(fallbackEmail) ? string.Empty : fallbackEmail.Trim();
    }

    private static List<SubmissionAuthorInputViewModel> NormalizePostedAuthors(IEnumerable<SubmissionAuthorInputViewModel>? authors)
    {
        if (authors is null)
            return new List<SubmissionAuthorInputViewModel>();

        return authors
            .Where(author => !string.IsNullOrWhiteSpace(author.FirstName)
                    || !string.IsNullOrWhiteSpace(author.LastName)
                    || !string.IsNullOrWhiteSpace(author.FullName))
            .Select(author => new SubmissionAuthorInputViewModel
            {
                Id = author.Id,
                TitleId = NormalizeOptionalGuid(author.TitleId),
                TitleName = string.IsNullOrWhiteSpace(author.TitleName) ? null : author.TitleName.Trim(),
                FirstName = ResolveAuthorFirstName(author),
                LastName = ResolveAuthorLastName(author),
                FullName = ResolveAuthorFullName(author),
                Email = string.IsNullOrWhiteSpace(author.Email) ? string.Empty : author.Email.Trim(),
                Institution = BackOfficeTextNormalizer.NormalizeInstitution(author.Institution) ?? string.Empty,
                Orcid = string.IsNullOrWhiteSpace(author.Orcid) ? null : author.Orcid.Trim(),
                IsCorrespondingAuthor = author.IsCorrespondingAuthor
            })
            .GroupBy(author => author.Id.HasValue && author.Id.Value != Guid.Empty
                ? $"id:{author.Id.Value}"
                : $"new:{author.FullName.ToUpperInvariant()}:{(author.Email ?? string.Empty).ToUpperInvariant()}")
            .Select(group => group.Last())
            .ToList();
    }


    private static string ResolveAuthorFullName(SubmissionAuthorInputViewModel author)
    {
        (string firstName, string lastName) = BackOfficeTextNormalizer.NormalizeAuthorNameParts(
            author.FirstName,
            author.LastName,
            author.FullName);

        return BackOfficeTextNormalizer.NormalizePersonFullName(firstName, lastName);
    }

    private static string ResolveAuthorFirstName(SubmissionAuthorInputViewModel author)
    {
        (string firstName, _) = BackOfficeTextNormalizer.NormalizeAuthorNameParts(
            author.FirstName,
            author.LastName,
            author.FullName);

        return firstName;
    }

    private static string ResolveAuthorLastName(SubmissionAuthorInputViewModel author)
    {
        (_, string lastName) = BackOfficeTextNormalizer.NormalizeAuthorNameParts(
            author.FirstName,
            author.LastName,
            author.FullName);

        return lastName;
    }

    private static Guid? NormalizeOptionalGuid(Guid? value)
    {
        return value.HasValue && value.Value != Guid.Empty ? value.Value : null;
    }

    private void AddApplicationValidationErrorsToModelState(ApplicationValidationException exception)
    {
        bool hasStructuredErrors = false;

        if (exception.Errors is not null)
        {
            foreach (var validationError in exception.Errors)
            {
                hasStructuredErrors = true;

                string key = NormalizeApplicationValidationPropertyName(validationError.Property);
                foreach (string error in validationError.Errors ?? Enumerable.Empty<string>())
                {
                    ModelState.AddModelError(key, ResolveApplicationValidationMessage(error));
                }
            }
        }

        if (!hasStructuredErrors)
        {
            ModelState.AddModelError(string.Empty, T("BackOffice.Submissions.Validation.GeneralError", "Bildiri bilgilerini kontrol edin. Hatalı veya eksik alanlar var."));
        }
    }

    private static string NormalizeApplicationValidationPropertyName(string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            return string.Empty;

        string key = propertyName.Trim();

        if (key.StartsWith("Command.", StringComparison.OrdinalIgnoreCase))
            key = key["Command.".Length..];

        return key;
    }

    private string ResolveApplicationValidationMessage(string? messageOrResourceKey)
    {
        if (string.IsNullOrWhiteSpace(messageOrResourceKey))
            return T("BackOffice.Submissions.Validation.GeneralError", "Bildiri bilgilerini kontrol edin. Hatalı veya eksik alanlar var.");

        string key = messageOrResourceKey.Trim();

        return key switch
        {
            "BackOffice.Submissions.Validation.OrcidMaxLength" => T(key, "ORCID en fazla 50 karakter olabilir."),
            "BackOffice.Submissions.Validation.AbstractMaxLength" => T(key, "Türkçe özet en fazla 25.000 karakter olabilir."),
            "BackOffice.Submissions.Validation.AbstractEnMaxLength" => T(key, "İngilizce özet en fazla 25.000 karakter olabilir."),
            "BackOffice.Submissions.Validation.TitleMaxLength" => T(key, "Bildiri başlığı en fazla izin verilen uzunlukta olmalıdır."),
            "BackOffice.Submissions.Validation.TitleEnMaxLength" => T(key, "İngilizce bildiri başlığı en fazla izin verilen uzunlukta olmalıdır."),
            "BackOffice.Submissions.Validation.KeywordsMaxLength" => T(key, "Anahtar kelimeler en fazla izin verilen uzunlukta olmalıdır."),
            "BackOffice.Submissions.Validation.KeywordsEnMaxLength" => T(key, "İngilizce anahtar kelimeler en fazla izin verilen uzunlukta olmalıdır."),
            _ => T(key, key)
        };
    }

    private void LocalizeSubmissionCreateModelState(SubmissionCreateViewModel model)
    {
        LocalizeRequiredModelState(nameof(model.SubmissionTypeId), model.SubmissionTypeId.HasValue, "BackOffice.Submissions.Create.Validation.SubmissionTypeRequired", "Bildiri türü zorunludur.");
        LocalizeRequiredModelState(nameof(model.TopicId), model.TopicId.HasValue, "BackOffice.Submissions.Create.Validation.TopicRequired", "Konu seçimi zorunludur.");
        LocalizeRequiredModelState(nameof(model.Title), !string.IsNullOrWhiteSpace(model.Title), "BackOffice.Submissions.Create.Validation.TitleRequired", "Bildiri başlığı zorunludur.");
        LocalizeRequiredModelState(nameof(model.Keywords), !string.IsNullOrWhiteSpace(model.Keywords), "BackOffice.Submissions.Create.Validation.KeywordsRequired", "Anahtar kelimeler zorunludur.");
        LocalizeRequiredModelState(nameof(model.Abstract), !string.IsNullOrWhiteSpace(model.Abstract), "BackOffice.Submissions.Create.Validation.AbstractRequired", "Özet zorunludur.");
    }

    private void LocalizeSubmissionUpdateModelState(SubmissionUpdateViewModel model)
    {
        LocalizeRequiredModelState(nameof(model.SubmissionTypeId), model.SubmissionTypeId.HasValue, "BackOffice.Submissions.Create.Validation.SubmissionTypeRequired", "Bildiri türü zorunludur.");
        LocalizeRequiredModelState(nameof(model.TopicId), model.TopicId.HasValue, "BackOffice.Submissions.Create.Validation.TopicRequired", "Konu seçimi zorunludur.");
        LocalizeRequiredModelState(nameof(model.Title), !string.IsNullOrWhiteSpace(model.Title), "BackOffice.Submissions.Create.Validation.TitleRequired", "Bildiri başlığı zorunludur.");
        LocalizeRequiredModelState(nameof(model.Keywords), !string.IsNullOrWhiteSpace(model.Keywords), "BackOffice.Submissions.Create.Validation.KeywordsRequired", "Anahtar kelimeler zorunludur.");
        LocalizeRequiredModelState(nameof(model.Abstract), !string.IsNullOrWhiteSpace(model.Abstract), "BackOffice.Submissions.Create.Validation.AbstractRequired", "Özet zorunludur.");
    }

    private void LocalizeRequiredModelState(string key, bool hasValue, string resourceKey, string fallback)
    {
        if (hasValue || !ModelState.ContainsKey(key))
            return;

        ModelState.Remove(key);
        ModelState.AddModelError(key, T(resourceKey, fallback));
    }

    private string T(string key, string fallback)
    {
        string value = _localizer.GetStringValue(key);
        return string.IsNullOrWhiteSpace(value) || string.Equals(value, key, StringComparison.OrdinalIgnoreCase)
            ? fallback
            : value;
    }

    private async Task<Submission?> LoadSubmissionForAuthorActionAsync(Guid submissionId, CancellationToken cancellationToken)
    {
        return await _submissionRepository
            .Query()
            .AsNoTracking()
            .Include(item => item.Authors)
            .Include(item => item.PaymentStatus)
            .Include(item => item.TransactionStatus)
            .FirstOrDefaultAsync(item => item.Id == submissionId && item.DeletedDate == null, cancellationToken);
    }

    private bool CanCurrentUserAccessSubmission(Submission submission)
    {
        if (CanCurrentUserManageSubmissions())
            return true;

        Guid? currentUserId = GetCurrentUserId();
        if (currentUserId.HasValue && submission.CreatedByUserId == currentUserId.Value)
            return true;

        string? currentEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(currentEmail) &&
            submission.Authors.Any(author =>
                !string.IsNullOrWhiteSpace(author.Email) &&
                string.Equals(author.Email.Trim(), currentEmail.Trim(), StringComparison.OrdinalIgnoreCase)))
            return true;

        return false;
    }

    private bool CanCurrentUserManageSubmissions()
    {
        return User.IsInRole("Admin") ||
               User.IsInRole("SuperAdmin") ||
               User.IsInRole("Editor") ||
               User.IsInRole("CongressEditor") ||
               User.IsInRole("OrganizationAdmin");
    }

    private static bool IsRejectedSubmission(Submission submission)
    {
        return string.Equals(
            submission.TransactionStatus?.Code,
            "REJECTED",
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool CanUploadPaymentDocumentForSubmission(Submission submission)
    {
        string statusCode = submission.TransactionStatus?.Code?.ToUpperInvariant() ?? string.Empty;
        return statusCode is "PAYMENT_PENDING" or "PAYMENT_DOCUMENT" or "PAYMENT";
    }

    private static bool IsAllowedPaymentDocumentExtension(string? extension)
    {
        return !string.IsNullOrWhiteSpace(extension) &&
               new[] { ".pdf", ".jpg", ".jpeg", ".png", ".webp" }
                   .Contains(extension.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    private async Task<IActionResult> UploadFinalSubmissionFileAsync(
        Guid submissionId,
        IFormFile? file,
        SubmissionFileKind fileKind,
        string folderName,
        long maxSize,
        HashSet<string> allowedExtensions,
        string requiredMessage,
        string extensionMessage,
        string sizeMessage,
        string successMessage,
        CancellationToken cancellationToken)
    {
        if (submissionId == Guid.Empty)
            return BadRequest();

        Submission? submission = await LoadSubmissionForAuthorActionAsync(submissionId, cancellationToken);
        if (submission is null)
            return NotFound();

        if (!CanCurrentUserAccessSubmission(submission))
            return Forbid();

        if (!CanCurrentUserManageSubmissions() && !CanUploadFinalFilesForSubmission(submission))
        {
            TempData["ErrorMessage"] = T("BackOffice.Submissions.FinalFiles.UploadLocked", "Tam metin ve video yükleme alanı ödeme işlemi tamamlandıktan sonra aktif olur.");
            return RedirectToAction(nameof(Index), new { culture = RouteData.Values["culture"]?.ToString() });
        }

        if (file is null || file.Length <= 0)
        {
            TempData["ErrorMessage"] = requiredMessage;
            return RedirectToAction(nameof(Index), new { culture = RouteData.Values["culture"]?.ToString() });
        }

        string extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
        {
            TempData["ErrorMessage"] = extensionMessage;
            return RedirectToAction(nameof(Index), new { culture = RouteData.Values["culture"]?.ToString() });
        }

        if (file.Length > maxSize)
        {
            TempData["ErrorMessage"] = sizeMessage;
            return RedirectToAction(nameof(Index), new { culture = RouteData.Values["culture"]?.ToString() });
        }

        string? bucketName = ResolveSubmissionBucketName();
        if (string.IsNullOrWhiteSpace(bucketName))
            return Problem("Submission file storage bucket is not configured.");

        string safeOriginalFileName = BuildFinalSubmissionFileName(submission, fileKind, extension);

        string objectName = BackOfficeObjectStorageHelper.BuildObjectName(
            "submissions",
            ResolveSubmissionStorageSegment(submission),
            "final-files",
            folderName,
            $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}");

        ObjectStorageUploadResult uploadResult;
        await using (Stream content = file.OpenReadStream())
        {
            uploadResult = await _objectStorageService.UploadAsync(
                new ObjectStorageUploadRequest
                {
                    BucketName = bucketName,
                    ObjectName = objectName,
                    OriginalFileName = safeOriginalFileName,
                    ContentType = BackOfficeObjectStorageHelper.NormalizeContentType(file.ContentType),
                    Size = file.Length,
                    Content = content,
                    Metadata = new Dictionary<string, string>
                    {
                        ["module"] = fileKind == SubmissionFileKind.FullText
                            ? "submission-author-full-text"
                            : "submission-author-presentation-video",
                        ["submission-id"] = submission.Id.ToString("D"),
                        ["congress-id"] = submission.CongressId.ToString("D")
                    }
                },
                cancellationToken);
        }

        try
        {
            await ReplaceFinalSubmissionFileAsync(
                submission.Id,
                fileKind,
                safeOriginalFileName,
                uploadResult.ObjectName,
                uploadResult.ContentType,
                uploadResult.Size,
                cancellationToken);
        }
        catch
        {
            await BackOfficeObjectStorageHelper.DeleteObjectIfExistsAsync(
                _objectStorageService,
                bucketName,
                uploadResult.ObjectName,
                cancellationToken);

            throw;
        }

        TempData["SuccessMessage"] = successMessage;
        return RedirectToAction(nameof(Index), new { culture = RouteData.Values["culture"]?.ToString() });
    }

    private async Task ReplaceFinalSubmissionFileAsync(
        Guid submissionId,
        SubmissionFileKind fileKind,
        string originalFileName,
        string filePath,
        string? contentType,
        long? fileSize,
        CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;
        string auditActor = GetCurrentUserId()?.ToString() ?? "SubmissionAuthor";

        List<SubmissionFile> activeFiles = await _submissionFileRepository
            .Query()
            .Where(item =>
                item.SubmissionId == submissionId &&
                item.FileKind == fileKind &&
                item.DeletedDate == null &&
                item.IsActive)
            .ToListAsync(cancellationToken);

        foreach (SubmissionFile activeFile in activeFiles)
        {
            activeFile.IsActive = false;
            activeFile.UpdatedDate = now;
            activeFile.UpdatedBy = auditActor;
            await _submissionFileRepository.UpdateAsync(activeFile);
        }

        int nextVersionNo = await _submissionFileRepository
            .Query()
            .IgnoreQueryFilters()
            .Where(item => item.SubmissionId == submissionId && item.FileKind == fileKind)
            .Select(item => (int?)item.VersionNo)
            .MaxAsync(cancellationToken) ?? 0;

        await _submissionFileRepository.AddAsync(new SubmissionFile
        {
            Id = Guid.NewGuid(),
            SubmissionId = submissionId,
            FileKind = fileKind,
            OriginalFileName = originalFileName,
            FilePath = filePath,
            ContentType = contentType,
            FileSize = fileSize,
            ReviewStatus = SubmissionFileReviewStatus.PendingReview,
            ReviewNote = null,
            ReviewedAt = null,
            ReviewedByUserId = null,
            IsIncludedInProgramBook = false,
            VersionNo = nextVersionNo + 1,
            IsActive = true,
            CreatedDate = now,
            CreatedBy = auditActor
        });
    }

    private static string BuildFinalSubmissionFileName(Submission submission, SubmissionFileKind fileKind, string extension)
    {
        string submissionCode = BuildFileNameSegment(
            string.IsNullOrWhiteSpace(submission.SubmissionNumber)
                ? submission.Id.ToString("N")[..8].ToUpperInvariant()
                : submission.SubmissionNumber);

        string fileTypeCode = fileKind switch
        {
            SubmissionFileKind.FullText => "FULL-TEXT",
            SubmissionFileKind.Presentation => "PRESENTATION-VIDEO",
            SubmissionFileKind.AcceptanceLetter => "ACCEPTANCE-LETTER",
            SubmissionFileKind.ExhibitionImage => "EXHIBITION-IMAGE",
            _ => "FILE"
        };

        string fileCode = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        string normalizedExtension = string.IsNullOrWhiteSpace(extension)
            ? string.Empty
            : extension.ToLowerInvariant();

        return $"{submissionCode}_{fileTypeCode}_{fileCode}{normalizedExtension}";
    }

    private static string BuildFileNameSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "SUBMISSION";

        string normalized = value.Trim().ToUpperInvariant()
            .Replace("İ", "I", StringComparison.Ordinal)
            .Replace("Ö", "O", StringComparison.Ordinal)
            .Replace("Ü", "U", StringComparison.Ordinal)
            .Replace("Ş", "S", StringComparison.Ordinal)
            .Replace("Ğ", "G", StringComparison.Ordinal)
            .Replace("Ç", "C", StringComparison.Ordinal);

        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"[^A-Z0-9]+", "-");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"-+", "-").Trim('-');

        return string.IsNullOrWhiteSpace(normalized) ? "SUBMISSION" : normalized;
    }

    private static string NormalizeCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string normalized = value.Trim().ToUpperInvariant()
            .Replace("İ", "I", StringComparison.Ordinal)
            .Replace("Ö", "O", StringComparison.Ordinal)
            .Replace("Ü", "U", StringComparison.Ordinal)
            .Replace("Ş", "S", StringComparison.Ordinal)
            .Replace("Ğ", "G", StringComparison.Ordinal)
            .Replace("Ç", "C", StringComparison.Ordinal);

        return new string(normalized.Where(char.IsLetterOrDigit).ToArray());
    }

    private static bool CanUploadFinalFilesForSubmission(Submission submission)
    {
        string paymentStatusCode = submission.PaymentStatus?.Code ?? string.Empty;
        string transactionStatusCode = submission.TransactionStatus?.Code ?? string.Empty;
        string normalizedPaymentStatus = NormalizeCode(paymentStatusCode);
        string normalizedTransactionStatus = NormalizeCode(transactionStatusCode);

        return normalizedPaymentStatus is "PAID" or "PAYMENTPAID" or "PAYMENTCOMPLETED" or "PAYMENTDONE" or "APPROVED" or "PAYMENTAPPROVED" or "COMPLETED" or "ODEMEYAPILDI" or "ODEMEISLEMIYAPILDI" or "ODEMEALINDI"
            || normalizedTransactionStatus is "COMPLETED";
    }

    private static string ResolveSubmissionStorageSegment(Submission submission)
    {
        return string.IsNullOrWhiteSpace(submission.SubmissionNumber)
            ? submission.Id.ToString("N")
            : submission.SubmissionNumber.Trim();
    }

    private async Task<SubmissionFile?> LoadSubmissionFileAsync(Guid fileId, CancellationToken cancellationToken)
    {
        return await _submissionFileRepository
            .Query()
            .AsNoTracking()
            .Include(item => item.Submission)
                .ThenInclude(submission => submission.Authors)
            .Include(item => item.Submission)
                .ThenInclude(submission => submission.TransactionStatus)
            .FirstOrDefaultAsync(item => item.Id == fileId && item.DeletedDate == null, cancellationToken);
    }

    private async Task<IActionResult> CreateSubmissionFileResultAsync(
        SubmissionFile file,
        CancellationToken cancellationToken)
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

        return File(stream, contentType, downloadName, enableRangeProcessing: true);
    }

    private string? ResolveSubmissionBucketName()
        => string.IsNullOrWhiteSpace(_objectStorageOptions.Buckets.Submissions)
            ? null
            : _objectStorageOptions.Buckets.Submissions.Trim();

    private bool CanCurrentUserAccessSubmissionFile(SubmissionFile file)
    {
        return CanCurrentUserAccessSubmission(file.Submission);
    }

    private static object ToDataTableRow(GetListSubmissionListItemDto item, int rowNumber)
    {
        DateTime? displayDate = item.SubmittedAt ?? item.UpdatedDate ?? item.CreatedDate;

        return new
        {
            rowNumber,
            id = item.Id,
            congressId = item.CongressId,
            submissionNumber = item.SubmissionNumber,
            title = item.Title,
            titleEn = item.TitleEn,
            submissionTypeName = item.SubmissionTypeName,
            topicName = item.TopicName,
            orcid = item.Orcid,
            correspondingAuthorName = item.CorrespondingAuthorName,
            otherAuthorsText = item.OtherAuthorsText,
            authorCount = item.AuthorCount,
            paymentStatusName = item.PaymentStatusName,
            paymentStatusBadgeClass = item.PaymentStatusBadgeClass,
            transactionStatusName = item.TransactionStatusName,
            transactionStatusBadgeClass = item.TransactionStatusBadgeClass,
            displayDate = FormatDate(displayDate),
            displayTime = FormatTime(displayDate),
            canEdit = item.CanEdit,
            canDelete = item.CanDelete
        };
    }

    private static string FormatDate(DateTime? value)
        => IsMeaningfulDate(value) ? value!.Value.ToString("dd.MM.yyyy") : "-";

    private static string FormatTime(DateTime? value)
        => IsMeaningfulDate(value) ? value!.Value.ToString("HH:mm") : "-";

    private static bool IsMeaningfulDate(DateTime? value)
        => value.HasValue && value.Value.Year >= 1900;

    private Guid? GetCurrentUserId()
    {
        string? rawId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(rawId, out Guid userId) ? userId : null;
    }

    private sealed record SubmissionCongressContext(Guid Id, string Name);

    private RedirectToActionResult RedirectToIndex()
    {
        string? culture = RouteData.Values["culture"]?.ToString();

        if (string.IsNullOrWhiteSpace(culture))
            return RedirectToAction(nameof(Index));

        return RedirectToAction(nameof(Index), new { culture });
    }
}
