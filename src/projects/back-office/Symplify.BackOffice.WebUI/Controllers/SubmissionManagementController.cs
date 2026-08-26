using System.Security.Claims;
using Core.Application.Storage;
using Core.Application.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Common.Storage;
using Symplify.BackOffice.Application.Features.Submissions.Commands.Create;
using Symplify.BackOffice.Application.Features.ExhibitionApplications.Commands.Create;
using Symplify.BackOffice.Application.Features.Submissions.Commands.Delete;
using Symplify.BackOffice.Application.Features.Submissions.Commands.Update;
using Symplify.BackOffice.Application.Common.Text;
using Symplify.BackOffice.Application.Features.Submissions.Queries.GetById;
using Symplify.BackOffice.Application.Features.Submissions.Queries.GetCreatePage;
using Symplify.BackOffice.Application.Features.Submissions.Commands.SaveEditorEvaluation;
using Symplify.BackOffice.Application.Features.Submissions.Queries.GetEditorEvaluationForm;
using Symplify.BackOffice.Application.Features.Submissions.Queries.GetList;
using Symplify.BackOffice.Application.Features.Submissions.Queries.GetManage;
using Symplify.BackOffice.Application.Features.Submissions.Queries.GetManagementFilterOptions;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Application.Features.ReviewerEvaluations.Constants;
using Symplify.BackOffice.WebUI.Models.Shared.DataTables;
using Symplify.BackOffice.WebUI.Models.Submissions;
using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture?}/submission-management")]
public sealed class SubmissionManagementController : Controller
{
    private const int DefaultPageIndex = 0;
    private const int DefaultPageSize = 250;
    private const int StatsPageSize = 100000;
    private const long MaxExhibitionFileSize = 20 * 1024 * 1024;
    private const long MaxFullTextFileSize = 50 * 1024 * 1024;
    private const long MaxPresentationVideoFileSize = 500L * 1024 * 1024;

    private static readonly HashSet<string> AllowedExhibitionFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".pdf"
    };

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
    private readonly IBackOfficeViewLocalizer _localizer;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ISubmissionFileRepository _submissionFileRepository;
    private readonly ObjectStorageOptions _objectStorageOptions;

    public SubmissionManagementController(
        IMediator mediator,
        IBackOfficeViewLocalizer localizer,
        IObjectStorageService objectStorageService,
        ISubmissionFileRepository submissionFileRepository,
        IOptions<ObjectStorageOptions> objectStorageOptions)
    {
        _mediator = mediator;
        _localizer = localizer;
        _objectStorageService = objectStorageService;
        _submissionFileRepository = submissionFileRepository;
        _objectStorageOptions = objectStorageOptions.Value;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? searchText,
        Guid? congressId,
        int? transactionStatusId,
        int? paymentStatusId,
        Guid? topicId,
        Guid? submissionTypeId,
        int? ownerMultiplicity,
        bool archiveMode = false,
        int page = DefaultPageIndex,
        int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        string? culture = RouteData.Values["culture"]?.ToString();

        var response = await _mediator.Send(new GetListSubmissionQuery
        {
            PageRequest = new PageRequest
            {
                Page = page < 0 ? DefaultPageIndex : page,
                PageSize = pageSize <= 0 ? DefaultPageSize : pageSize
            },
            RequestedByUserId = GetCurrentUserId(),
            CanManageAllSubmissions = CanCurrentUserManageAllSubmissions(),
            Culture = culture,
            SearchText = NormalizeSearchText(searchText),
            CongressId = NormalizeGuid(congressId),
            ArchiveMode = archiveMode,
            TransactionStatusId = NormalizeTransactionStatusId(transactionStatusId),
            PaymentStatusId = NormalizePaymentStatusId(paymentStatusId),
            TopicId = NormalizeGuid(topicId),
            SubmissionTypeId = NormalizeGuid(submissionTypeId),
            OwnerMultiplicity = NormalizeOwnerMultiplicity(ownerMultiplicity),
            SortColumn = "submittedAt",
            SortDirection = "desc"
        }, cancellationToken);

        GetSubmissionManagementFilterOptionsResponse filterOptions = await _mediator.Send(new GetSubmissionManagementFilterOptionsQuery
        {
            Culture = culture,
            ArchiveMode = archiveMode
        }, cancellationToken);

        return View(new SubmissionManagementIndexViewModel
        {
            Submissions = response,
            FilterOptions = filterOptions,
            SearchText = NormalizeSearchText(searchText),
            CongressId = NormalizeGuid(congressId),
            TransactionStatusId = NormalizeTransactionStatusId(transactionStatusId),
            PaymentStatusId = NormalizePaymentStatusId(paymentStatusId),
            TopicId = NormalizeGuid(topicId),
            SubmissionTypeId = NormalizeGuid(submissionTypeId),
            OwnerMultiplicity = NormalizeOwnerMultiplicity(ownerMultiplicity),
            ArchiveMode = archiveMode
        });
    }

    [HttpPost("get-list")]
    [HttpPost("GetList")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GetList(
        [FromForm] DataTableRequest request,
        [FromForm] string? searchText,
        [FromForm] Guid? congressId,
        [FromForm] int? transactionStatusId,
        [FromForm] int? paymentStatusId,
        [FromForm] Guid? topicId,
        [FromForm] Guid? submissionTypeId,
        [FromForm] int? ownerMultiplicity,
        [FromForm] bool archiveMode,
        CancellationToken cancellationToken)
    {
        DataTableQueryOptions tableOptions = DataTableQueryOptions.From(
            request,
            defaultSortColumn: "submittedAt",
            defaultSortDirection: "desc",
            allowedSortColumns: new[]
            {
                "submittedAt",
                "submission",
                "title",
                "submissionNumber",
                "congress",
                "typeTopic",
                "owner",
                "authors",
                "payment",
                "status"
            });

        string? effectiveSearchText = NormalizeSearchText(searchText) ?? tableOptions.SearchText;
        Guid? normalizedCongressId = NormalizeGuid(congressId);
        int? normalizedTransactionStatusId = NormalizeTransactionStatusId(transactionStatusId);
        int? normalizedPaymentStatusId = NormalizePaymentStatusId(paymentStatusId);
        Guid? normalizedTopicId = NormalizeGuid(topicId);
        Guid? normalizedSubmissionTypeId = NormalizeGuid(submissionTypeId);
        SubmissionOwnerMultiplicityFilter normalizedOwnerMultiplicity = NormalizeOwnerMultiplicity(ownerMultiplicity);
        string? culture = RouteData.Values["culture"]?.ToString();

        var response = await _mediator.Send(new GetListSubmissionQuery
        {
            PageRequest = new PageRequest
            {
                Page = tableOptions.Page,
                PageSize = tableOptions.PageSize
            },
            RequestedByUserId = GetCurrentUserId(),
            CanManageAllSubmissions = CanCurrentUserManageAllSubmissions(),
            Culture = culture,
            SearchText = effectiveSearchText,
            CongressId = normalizedCongressId,
            ArchiveMode = archiveMode,
            TransactionStatusId = normalizedTransactionStatusId,
            PaymentStatusId = normalizedPaymentStatusId,
            TopicId = normalizedTopicId,
            SubmissionTypeId = normalizedSubmissionTypeId,
            OwnerMultiplicity = normalizedOwnerMultiplicity,
            SortColumn = tableOptions.SortColumn,
            SortDirection = tableOptions.SortDirection
        }, cancellationToken);

        List<object> pageItems = response.Items
            .Select((item, index) => ToDataTableRow(item, tableOptions.Start + index + 1))
            .Cast<object>()
            .ToList();

        var statsResponse = await _mediator.Send(new GetListSubmissionQuery
        {
            PageRequest = new PageRequest
            {
                Page = 0,
                PageSize = StatsPageSize
            },
            RequestedByUserId = GetCurrentUserId(),
            CanManageAllSubmissions = CanCurrentUserManageAllSubmissions(),
            Culture = culture,
            SearchText = effectiveSearchText,
            CongressId = normalizedCongressId,
            ArchiveMode = archiveMode,
            TransactionStatusId = normalizedTransactionStatusId,
            PaymentStatusId = normalizedPaymentStatusId,
            TopicId = normalizedTopicId,
            SubmissionTypeId = normalizedSubmissionTypeId,
            OwnerMultiplicity = normalizedOwnerMultiplicity,
            SortColumn = tableOptions.SortColumn,
            SortDirection = tableOptions.SortDirection
        }, cancellationToken);

        return Json(new
        {
            draw = request.Draw,
            recordsTotal = response.Count,
            recordsFiltered = response.Count,
            data = pageItems,
            stats = BuildStats(statsResponse.Items)
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Manage(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest();

        GetManageSubmissionResponse detail = await _mediator.Send(new GetManageSubmissionQuery
        {
            Id = id,
            PerformedByUserId = GetCurrentUserId(),
            Culture = RouteData.Values["culture"]?.ToString()
        }, cancellationToken);

        return View(new SubmissionManageViewModel
        {
            Detail = detail
        });
    }

    [HttpGet("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, string? returnUrl, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest();

        if (!CanCurrentUserManageAllSubmissions())
            return Forbid();

        ViewData["ReturnUrl"] = NormalizeLocalReturnUrl(returnUrl);

        SubmissionUpdateViewModel model = await BuildManagementUpdateViewModelAsync(id, null, cancellationToken);
        return View(model);
    }

    [HttpPost("edit/{id:guid}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(500L * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 500L * 1024 * 1024)]
    public async Task<IActionResult> Edit(Guid id, SubmissionUpdateViewModel model, [FromQuery] string? returnUrl, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty || model.Id == Guid.Empty || id != model.Id)
            return BadRequest();

        if (!CanCurrentUserManageAllSubmissions())
            return Forbid();

        ViewData["ReturnUrl"] = NormalizeLocalReturnUrl(returnUrl);

        GetByIdSubmissionResponse currentSubmission = await _mediator.Send(new GetByIdSubmissionQuery
        {
            Id = id,
            Culture = RouteData.Values["culture"]?.ToString()
        }, cancellationToken);

        model.IsExhibitionApplication = currentSubmission.IsExhibitionApplication;
        model.SubmissionTypeId = currentSubmission.SubmissionTypeId;
        model.Authors = model.IsExhibitionApplication
            ? MapExistingAuthorsForPost(currentSubmission)
            : NormalizePostedAuthors(model.Authors);

        if (model.IsExhibitionApplication)
            RemoveAuthorModelStateEntries();

        ValidateManagementUpdateModel(model);
        ValidateOptionalExhibitionFile(model);
        ValidateOptionalFinalFiles(model);

        if (!ModelState.IsValid)
        {
            model = await BuildManagementUpdateViewModelAsync(id, model, cancellationToken);
            return View(model);
        }

        ExhibitionApplicationFileInputDto? exhibitionFile = null;
        SubmissionManagementFileUploadInput? fullTextFile = null;
        SubmissionManagementFileUploadInput? presentationVideoFile = null;
        List<string?> uploadedObjectNames = new();

        try
        {
            if (model.IsExhibitionApplication && model.ExhibitionFile is { Length: > 0 })
            {
                exhibitionFile = await UploadExhibitionFileAsync(model, cancellationToken);
                uploadedObjectNames.Add(exhibitionFile.FilePath);
            }

            if (model.FullTextFile is { Length: > 0 })
            {
                fullTextFile = await UploadFinalFileAsync(model, model.FullTextFile, SubmissionFileKind.FullText, "full-text", cancellationToken);
                uploadedObjectNames.Add(fullTextFile.FilePath);
            }

            if (model.PresentationFile is { Length: > 0 })
            {
                presentationVideoFile = await UploadFinalFileAsync(model, model.PresentationFile, SubmissionFileKind.Presentation, "presentation-video", cancellationToken);
                uploadedObjectNames.Add(presentationVideoFile.FilePath);
            }

            await _mediator.Send(new UpdateSubmissionCommand
            {
                Id = model.Id,
                SubmissionTypeId = model.SubmissionTypeId,
                TopicId = model.IsExhibitionApplication ? null : model.TopicId,
                LanguageId = model.LanguageId,
                RequestedByUserId = GetCurrentUserId(),
                RequestedByCanManageAllSubmissions = true,
                IsExhibitionApplication = model.IsExhibitionApplication,
                Orcid = model.IsExhibitionApplication ? null : model.Orcid,
                Title = model.IsExhibitionApplication ? model.WorkName : model.Title,
                TitleEn = model.IsExhibitionApplication ? null : model.TitleEn,
                Abstract = model.IsExhibitionApplication ? model.Description : model.Abstract,
                AbstractEn = model.IsExhibitionApplication ? null : model.AbstractEn,
                Keywords = model.IsExhibitionApplication ? model.Technique : model.Keywords,
                KeywordsEn = model.IsExhibitionApplication ? null : model.KeywordsEn,
                WorkName = model.WorkName,
                Dimensions = model.Dimensions,
                Technique = model.Technique,
                Description = model.Description,
                Address = model.Address,
                ExhibitionFile = exhibitionFile,
                SubmitForReview = false,
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

            if (fullTextFile is not null)
                await ReplaceFinalSubmissionFileAsync(model.Id, fullTextFile, SubmissionFileKind.FullText, cancellationToken);

            if (presentationVideoFile is not null)
                await ReplaceFinalSubmissionFileAsync(model.Id, presentationVideoFile, SubmissionFileKind.Presentation, cancellationToken);
        }
        catch
        {
            foreach (string? uploadedObjectName in uploadedObjectNames)
            {
                await BackOfficeObjectStorageHelper.DeleteObjectIfExistsAsync(
                    _objectStorageService,
                    ResolveSubmissionBucketName(),
                    uploadedObjectName,
                    cancellationToken);
            }

            throw;
        }

        TempData["SuccessMessage"] = _localizer.GetStringValue("BackOffice.Submissions.Management.Edit.Success.Updated");

        if (model.IsExhibitionApplication)
        {
            return RedirectToAction(nameof(Edit), new
            {
                culture = RouteData.Values["culture"]?.ToString(),
                id = model.Id,
                returnUrl = NormalizeLocalReturnUrl(returnUrl)
            });
        }

        return RedirectToLocalReturnUrlOrIndex(returnUrl);
    }

    [HttpPost("delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(DeleteSubmissionCommand command, [FromForm] string? returnUrl, CancellationToken cancellationToken)
    {
        if (!CanCurrentUserManageAllSubmissions())
            return Forbid();

        command.RequestedByUserId = GetCurrentUserId();
        command.RequestedByCanManageAllSubmissions = true;

        await _mediator.Send(command, cancellationToken);

        TempData["SuccessMessage"] = _localizer.GetStringValue("BackOffice.Submissions.Management.Edit.Success.Deleted");
        return RedirectToLocalReturnUrlOrIndex(returnUrl);
    }

    private async Task<SubmissionUpdateViewModel> BuildManagementUpdateViewModelAsync(
        Guid id,
        SubmissionUpdateViewModel? postedModel,
        CancellationToken cancellationToken)
    {
        GetByIdSubmissionResponse submission = await _mediator.Send(new GetByIdSubmissionQuery
        {
            Id = id,
            Culture = RouteData.Values["culture"]?.ToString()
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
            IsExhibitionApplication = submission.IsExhibitionApplication,
            WorkName = submission.ExhibitionDetail?.WorkName ?? submission.Title,
            Dimensions = submission.ExhibitionDetail?.Dimensions,
            Technique = submission.ExhibitionDetail?.Technique ?? submission.Keywords ?? string.Empty,
            Description = submission.ExhibitionDetail?.Description ?? submission.Abstract,
            Address = submission.ExhibitionDetail?.Address ?? string.Empty,
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
        model.CanEdit = true;
        model.TransactionStatusName = submission.TransactionStatusName;
        model.TransactionStatusCode = submission.TransactionStatusCode;
        model.SubmittedAt = submission.SubmittedAt;
        model.UpdatedDate = submission.UpdatedDate;
        model.IsExhibitionApplication = submission.IsExhibitionApplication;

        if (postedModel is null && model.IsExhibitionApplication)
        {
            model.WorkName = submission.ExhibitionDetail?.WorkName ?? submission.Title;
            model.Dimensions = submission.ExhibitionDetail?.Dimensions;
            model.Technique = submission.ExhibitionDetail?.Technique ?? submission.Keywords ?? string.Empty;
            model.Description = submission.ExhibitionDetail?.Description ?? submission.Abstract;
            model.Address = submission.ExhibitionDetail?.Address ?? string.Empty;
        }

        AssignExistingExhibitionFile(model, submission);
        AssignExistingFinalFiles(model, submission);

        GetSubmissionCreatePageResponse createPage = await _mediator.Send(new GetSubmissionCreatePageQuery
        {
            CongressId = submission.CongressId,
            Culture = RouteData.Values["culture"]?.ToString()
        }, cancellationToken);

        model.CongressName = createPage.Congresses.FirstOrDefault(item => item.Id == submission.CongressId)?.Text
            ?? submission.CongressName
            ?? submission.CongressId.ToString();
        model.SubmissionTypes = createPage.SubmissionTypes.Select(MapSelectItem).ToList();
        model.Topics = createPage.Topics.Select(MapSelectItem).ToList();
        model.Languages = createPage.Languages.Select(MapSelectItem).ToList();
        model.TitleOptions = createPage.Titles.Select(MapSelectItem).ToList();

        return model;
    }

    private static void AssignExistingExhibitionFile(SubmissionUpdateViewModel model, GetByIdSubmissionResponse submission)
    {
        SubmissionDetailFileDto? exhibitionFile = submission.Files
            .Where(file => file.FileKind == SubmissionFileKind.ExhibitionImage && file.IsActive)
            .OrderByDescending(file => file.DisplayDate ?? file.UploadedAt)
            .FirstOrDefault();

        if (exhibitionFile is null)
        {
            model.ExistingExhibitionFileId = null;
            model.ExistingExhibitionFileKind = null;
            model.ExistingExhibitionFileName = null;
            model.ExistingExhibitionFileContentType = null;
            model.ExistingExhibitionFileSize = null;
            model.ExistingExhibitionFileUploadedAt = null;
            return;
        }

        model.ExistingExhibitionFileId = exhibitionFile.Id;
        model.ExistingExhibitionFileKind = exhibitionFile.FileKind;
        model.ExistingExhibitionFileName = exhibitionFile.OriginalFileName;
        model.ExistingExhibitionFileContentType = exhibitionFile.ContentType;
        model.ExistingExhibitionFileSize = exhibitionFile.FileSize;
        model.ExistingExhibitionFileUploadedAt = exhibitionFile.DisplayDate ?? exhibitionFile.UploadedAt;
    }


    private static void AssignExistingFinalFiles(SubmissionUpdateViewModel model, GetByIdSubmissionResponse submission)
    {
        SubmissionDetailFileDto? fullTextFile = submission.Files
            .Where(file => file.FileKind == SubmissionFileKind.FullText && file.IsActive)
            .OrderByDescending(file => file.DisplayDate ?? file.UploadedAt)
            .FirstOrDefault();

        SubmissionDetailFileDto? presentationFile = submission.Files
            .Where(file => file.FileKind == SubmissionFileKind.Presentation && file.IsActive)
            .OrderByDescending(file => file.DisplayDate ?? file.UploadedAt)
            .FirstOrDefault();

        model.ExistingFullTextFileId = fullTextFile?.Id;
        model.ExistingFullTextFileName = fullTextFile?.OriginalFileName;
        model.ExistingFullTextFileContentType = fullTextFile?.ContentType;
        model.ExistingFullTextFileSize = fullTextFile?.FileSize;
        model.ExistingFullTextFileUploadedAt = fullTextFile?.DisplayDate ?? fullTextFile?.UploadedAt;

        model.ExistingPresentationFileId = presentationFile?.Id;
        model.ExistingPresentationFileName = presentationFile?.OriginalFileName;
        model.ExistingPresentationFileContentType = presentationFile?.ContentType;
        model.ExistingPresentationFileSize = presentationFile?.FileSize;
        model.ExistingPresentationFileUploadedAt = presentationFile?.DisplayDate ?? presentationFile?.UploadedAt;
    }

    private async Task<ExhibitionApplicationFileInputDto> UploadExhibitionFileAsync(SubmissionUpdateViewModel model, CancellationToken cancellationToken)
    {
        IFormFile file = model.ExhibitionFile ?? throw new InvalidOperationException(Localize("BackOffice.ExhibitionApplications.Create.Validation.FileRequired", "Sergi görseli yüklenmelidir."));
        string? bucketName = ResolveSubmissionBucketName();

        if (string.IsNullOrWhiteSpace(bucketName))
            throw new InvalidOperationException("Submission file storage bucket is not configured.");

        string extension = Path.GetExtension(file.FileName);
        string safeOriginalFileName = string.IsNullOrWhiteSpace(file.FileName)
            ? $"exhibition-file{extension}"
            : Path.GetFileName(file.FileName);

        string objectName = BackOfficeObjectStorageHelper.BuildObjectName(
            "submissions",
            "exhibition-applications",
            model.Id.ToString("N"),
            $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}");

        await using Stream content = file.OpenReadStream();
        ObjectStorageUploadResult uploadResult = await _objectStorageService.UploadAsync(
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
                    ["module"] = "submission-management-exhibition-application",
                    ["submission-id"] = model.Id.ToString("D"),
                    ["congress-id"] = model.CongressId.ToString("D")
                }
            },
            cancellationToken);

        return new ExhibitionApplicationFileInputDto
        {
            OriginalFileName = safeOriginalFileName,
            FilePath = uploadResult.ObjectName,
            ContentType = uploadResult.ContentType,
            FileSize = uploadResult.Size
        };
    }

    private void ValidateOptionalExhibitionFile(SubmissionUpdateViewModel model)
    {
        if (!model.IsExhibitionApplication || model.ExhibitionFile is null || model.ExhibitionFile.Length <= 0)
            return;

        if (model.ExhibitionFile.Length > MaxExhibitionFileSize)
            ModelState.AddModelError(nameof(model.ExhibitionFile), Localize("BackOffice.ExhibitionApplications.Create.Validation.FileSize", "Sergi dosyası en fazla 20 MB olabilir."));

        string extension = Path.GetExtension(model.ExhibitionFile.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExhibitionFileExtensions.Contains(extension))
            ModelState.AddModelError(nameof(model.ExhibitionFile), Localize("BackOffice.ExhibitionApplications.Create.Validation.FileType", "Sergi dosyası JPG, PNG, WEBP veya PDF formatında olmalıdır."));
    }


    private void ValidateOptionalFinalFiles(SubmissionUpdateViewModel model)
    {
        ValidateOptionalFinalFile(
            nameof(model.FullTextFile),
            model.FullTextFile,
            MaxFullTextFileSize,
            AllowedFullTextFileExtensions,
            Localize("BackOffice.Submissions.FinalFiles.Validation.FullTextSize", "Tam metin dosyası en fazla 50 MB olabilir."),
            Localize("BackOffice.Submissions.FinalFiles.Validation.FullTextType", "Tam metin dosyası yalnızca DOCX formatında olmalıdır."));

        ValidateOptionalFinalFile(
            nameof(model.PresentationFile),
            model.PresentationFile,
            MaxPresentationVideoFileSize,
            AllowedPresentationVideoFileExtensions,
            Localize("BackOffice.Submissions.FinalFiles.Validation.VideoSize", "Video sunum dosyası en fazla 500 MB olabilir."),
            Localize("BackOffice.Submissions.FinalFiles.Validation.VideoType", "Video sunum dosyası MP4, MOV veya WEBM formatında olmalıdır."));
    }

    private void ValidateOptionalFinalFile(
        string modelStateKey,
        IFormFile? file,
        long maxSize,
        HashSet<string> allowedExtensions,
        string sizeMessage,
        string extensionMessage)
    {
        if (file is null || file.Length <= 0)
            return;

        if (file.Length > maxSize)
            ModelState.AddModelError(modelStateKey, sizeMessage);

        string extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
            ModelState.AddModelError(modelStateKey, extensionMessage);
    }

    private async Task<SubmissionManagementFileUploadInput> UploadFinalFileAsync(
        SubmissionUpdateViewModel model,
        IFormFile file,
        SubmissionFileKind fileKind,
        string folderName,
        CancellationToken cancellationToken)
    {
        string? bucketName = ResolveSubmissionBucketName();
        if (string.IsNullOrWhiteSpace(bucketName))
            throw new InvalidOperationException("Submission file storage bucket is not configured.");

        string extension = Path.GetExtension(file.FileName);
        string safeOriginalFileName = BuildFinalSubmissionFileName(model, fileKind, extension);

        string objectName = BackOfficeObjectStorageHelper.BuildObjectName(
            "submissions",
            ResolveSubmissionStorageSegment(model),
            "final-files",
            folderName,
            $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}");

        await using Stream content = file.OpenReadStream();
        ObjectStorageUploadResult uploadResult = await _objectStorageService.UploadAsync(
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
                        ? "submission-management-full-text"
                        : "submission-management-presentation-video",
                    ["submission-id"] = model.Id.ToString("D"),
                    ["congress-id"] = model.CongressId.ToString("D")
                }
            },
            cancellationToken);

        return new SubmissionManagementFileUploadInput(
            safeOriginalFileName,
            uploadResult.ObjectName,
            uploadResult.ContentType,
            uploadResult.Size);
    }

    private async Task ReplaceFinalSubmissionFileAsync(
        Guid submissionId,
        SubmissionManagementFileUploadInput file,
        SubmissionFileKind fileKind,
        CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;
        string auditActor = GetCurrentUserId()?.ToString() ?? "SubmissionManagement";

        List<Symplify.BackOffice.Domain.Submission.SubmissionFile> activeFiles = await _submissionFileRepository
            .Query()
            .Where(item =>
                item.SubmissionId == submissionId &&
                item.FileKind == fileKind &&
                item.DeletedDate == null &&
                item.IsActive)
            .ToListAsync(cancellationToken);

        foreach (Symplify.BackOffice.Domain.Submission.SubmissionFile activeFile in activeFiles)
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

        await _submissionFileRepository.AddAsync(new Symplify.BackOffice.Domain.Submission.SubmissionFile
        {
            Id = Guid.NewGuid(),
            SubmissionId = submissionId,
            FileKind = fileKind,
            OriginalFileName = file.OriginalFileName,
            FilePath = file.FilePath,
            ContentType = file.ContentType,
            FileSize = file.FileSize,
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

    private static string BuildFinalSubmissionFileName(SubmissionUpdateViewModel model, SubmissionFileKind fileKind, string extension)
    {
        string submissionCode = BuildFileNameSegment(
            string.IsNullOrWhiteSpace(model.SubmissionNumber)
                ? model.Id.ToString("N")[..8].ToUpperInvariant()
                : model.SubmissionNumber);

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

    private static string ResolveSubmissionStorageSegment(SubmissionUpdateViewModel model)
    {
        return string.IsNullOrWhiteSpace(model.SubmissionNumber)
            ? model.Id.ToString("N")
            : model.SubmissionNumber.Trim();
    }

    private string? ResolveSubmissionBucketName()
        => string.IsNullOrWhiteSpace(_objectStorageOptions.Buckets.Submissions)
            ? null
            : _objectStorageOptions.Buckets.Submissions.Trim();

    private void ValidateManagementUpdateModel(SubmissionUpdateViewModel model)
    {
        if (model.IsExhibitionApplication)
        {
            RemoveAcademicModelStateEntries();

            if (string.IsNullOrWhiteSpace(model.WorkName))
                ModelState.AddModelError(nameof(model.WorkName), Localize("BackOffice.ExhibitionApplications.Create.Validation.WorkNameRequired", "Eser adı zorunludur."));

            if (string.IsNullOrWhiteSpace(model.Technique))
                ModelState.AddModelError(nameof(model.Technique), Localize("BackOffice.ExhibitionApplications.Create.Validation.TechniqueRequired", "Uygulanan teknik zorunludur."));

            if (string.IsNullOrWhiteSpace(model.Address))
                ModelState.AddModelError(nameof(model.Address), Localize("BackOffice.ExhibitionApplications.Create.Validation.AddressRequired", "Adres bilgisi zorunludur."));
        }
        else
        {
            if (!model.TopicId.HasValue || model.TopicId.Value == Guid.Empty)
                ModelState.AddModelError(nameof(model.TopicId), Localize("BackOffice.Submissions.Create.Validation.TopicRequired", "Konu seçimi zorunludur."));

            if (string.IsNullOrWhiteSpace(model.Title))
                ModelState.AddModelError(nameof(model.Title), Localize("BackOffice.Submissions.Create.Validation.TitleRequired", "Bildiri başlığı zorunludur."));

            if (string.IsNullOrWhiteSpace(model.Keywords))
                ModelState.AddModelError(nameof(model.Keywords), Localize("BackOffice.Submissions.Create.Validation.KeywordsRequired", "Anahtar kelimeler zorunludur."));

            if (string.IsNullOrWhiteSpace(model.Abstract))
                ModelState.AddModelError(nameof(model.Abstract), Localize("BackOffice.Submissions.Create.Validation.AbstractRequired", "Özet zorunludur."));
        }

        if (!model.Authors.Any(author => author.IsCorrespondingAuthor))
        {
            ModelState.AddModelError(string.Empty, Localize("BackOffice.Submissions.Management.Edit.Validation.CorrespondingAuthorRequired", "En az bir sorumlu yazar eklenmelidir."));
        }

        if (!model.IsExhibitionApplication)
        {
            if (model.Authors.Any(author => !author.TitleId.HasValue || author.TitleId.Value == Guid.Empty))
                ModelState.AddModelError(nameof(model.Authors), Localize("BackOffice.Submissions.Management.Edit.Validation.AuthorTitleRequired", "Her yazar için unvan seçilmelidir."));

            if (HasDuplicateAuthorEmail(model.Authors))
                ModelState.AddModelError(nameof(model.Authors), Localize("BackOffice.Submissions.AuthorForm.Validation.DuplicateEmail", "Bu e-posta adresiyle bir yazar zaten eklenmiş. Aynı e-posta adresiyle ikinci bir yazar eklenemez."));
        }
    }

    private void RemoveAcademicModelStateEntries()
    {
        ModelState.Remove(nameof(SubmissionUpdateViewModel.TopicId));
        ModelState.Remove(nameof(SubmissionUpdateViewModel.Title));
        ModelState.Remove(nameof(SubmissionUpdateViewModel.TitleEn));
        ModelState.Remove(nameof(SubmissionUpdateViewModel.Abstract));
        ModelState.Remove(nameof(SubmissionUpdateViewModel.AbstractEn));
        ModelState.Remove(nameof(SubmissionUpdateViewModel.Keywords));
        ModelState.Remove(nameof(SubmissionUpdateViewModel.KeywordsEn));
        ModelState.Remove(nameof(SubmissionUpdateViewModel.Orcid));
    }

    private void RemoveAuthorModelStateEntries()
    {
        foreach (string key in ModelState.Keys
            .Where(key =>
                key.Equals(nameof(SubmissionUpdateViewModel.Authors), StringComparison.OrdinalIgnoreCase) ||
                key.StartsWith($"{nameof(SubmissionUpdateViewModel.Authors)}[", StringComparison.OrdinalIgnoreCase) ||
                key.StartsWith($"{nameof(SubmissionUpdateViewModel.Authors)}.", StringComparison.OrdinalIgnoreCase))
            .ToList())
        {
            ModelState.Remove(key);
        }
    }

    private static bool HasDuplicateAuthorEmail(IEnumerable<SubmissionAuthorInputViewModel> authors)
    {
        HashSet<string> emails = new(StringComparer.OrdinalIgnoreCase);

        foreach (SubmissionAuthorInputViewModel author in authors)
        {
            if (string.IsNullOrWhiteSpace(author.Email))
                continue;

            if (!emails.Add(author.Email.Trim()))
                return true;
        }

        return false;
    }

    private string Localize(string key, string fallback)
    {
        string value = _localizer.GetStringValue(key);
        return string.IsNullOrWhiteSpace(value) || string.Equals(value, key, StringComparison.OrdinalIgnoreCase)
            ? fallback
            : value;
    }

    private static SubmissionCreateSelectItemViewModel MapSelectItem(SubmissionCreateSelectItemDto item)
    {
        return new SubmissionCreateSelectItemViewModel
        {
            Id = item.Id,
            Text = item.Text
        };
    }

    private static Guid? NormalizeOptionalGuid(Guid? value)
    {
        return value.HasValue && value.Value != Guid.Empty ? value.Value : null;
    }

    private static List<SubmissionAuthorInputViewModel> MapExistingAuthorsForPost(GetByIdSubmissionResponse submission)
    {
        return submission.Authors
            .Select(author => new SubmissionAuthorInputViewModel
            {
                Id = author.Id,
                TitleId = NormalizeOptionalGuid(author.TitleId),
                TitleName = string.IsNullOrWhiteSpace(author.TitleName) ? null : author.TitleName.Trim(),
                FirstName = BackOfficeTextNormalizer.NormalizePersonFirstName(author.FirstName),
                LastName = BackOfficeTextNormalizer.NormalizePersonSurname(author.LastName),
                FullName = BackOfficeTextNormalizer.NormalizePersonFullName(author.FirstName, author.LastName),
                Email = string.IsNullOrWhiteSpace(author.Email) ? string.Empty : author.Email.Trim(),
                Institution = BackOfficeTextNormalizer.NormalizeInstitution(author.Institution),
                Orcid = string.IsNullOrWhiteSpace(author.Orcid) ? null : author.Orcid.Trim(),
                IsCorrespondingAuthor = author.IsCorrespondingAuthor
            })
            .Where(author => !string.IsNullOrWhiteSpace(author.FirstName)
                    || !string.IsNullOrWhiteSpace(author.LastName)
                    || !string.IsNullOrWhiteSpace(author.FullName))
            .ToList();
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
                Institution = BackOfficeTextNormalizer.NormalizeInstitution(author.Institution),
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


    [HttpGet("{id:guid}/editor-evaluation")]
    public async Task<IActionResult> EditorEvaluation(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest();

        var response = await _mediator.Send(new GetEditorEvaluationFormQuery
        {
            SubmissionId = id,
            CurrentUserId = GetCurrentUserId(),
            Culture = RouteData.Values["culture"]?.ToString()
        }, cancellationToken);

        string? culture = RouteData.Values["culture"]?.ToString();
        ViewData["EvaluationFormController"] = "SubmissionManagement";
        ViewData["EvaluationFormAction"] = nameof(EditorEvaluation);
        ViewData["EvaluationFormRouteId"] = id;
        ViewData["EvaluationBackUrl"] = Url.Action(nameof(Manage), "SubmissionManagement", new { culture, id });
        ViewData["EvaluationBackText"] = _localizer.GetStringValue("BackOffice.Submissions.Management.Edit.Evaluation.BackToDetail");
        ViewData["EvaluationSubmitOnly"] = true;
        ViewData["EvaluationPageTitleOverride"] = _localizer.GetStringValue("BackOffice.Submissions.Management.Edit.Evaluation.PageTitle");

        return View("~/Views/ReviewerEvaluations/Evaluate.cshtml", response);
    }

    [HttpPost("{id:guid}/editor-evaluation")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditorEvaluation(Guid id, SaveEditorEvaluationCommand command, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty || command.EvaluationId == Guid.Empty)
            return BadRequest();

        command.CurrentUserId = GetCurrentUserId();

        try
        {
            SavedEditorEvaluationResponse response = await _mediator.Send(command, cancellationToken);
            TempData["SuccessMessage"] = ReviewerEvaluationResourceKeys.MessageSubmitted;
            return RedirectToAction(nameof(Manage), new { culture = RouteData.Values["culture"]?.ToString(), id = response.SubmissionId });
        }
        catch (Exception exception)
        {
            TempData["ErrorMessage"] = exception.Message;
            return RedirectToAction(nameof(EditorEvaluation), new { culture = RouteData.Values["culture"]?.ToString(), id });
        }
    }

    private static object ToDataTableRow(GetListSubmissionListItemDto item, int rowNumber)
    {
        DateTime? displayDate = item.SubmittedAt ?? item.UpdatedDate ?? item.CreatedDate;

        return new
        {
            rowNumber,
            id = item.Id,
            congressId = item.CongressId,
            congressCode = item.CongressCode,
            congressName = item.CongressName,
            submissionNumber = item.SubmissionNumber,
            title = item.Title,
            titleEn = item.TitleEn,
            submissionTypeName = item.SubmissionTypeName,
            topicName = item.TopicName,
            orcid = item.Orcid,
            submissionOwnerName = item.SubmissionOwnerName,
            submissionOwnerEmail = item.SubmissionOwnerEmail,
            ownerSubmissionCount = item.OwnerSubmissionCount,
            hasMultipleSubmissions = item.HasMultipleSubmissions,
            correspondingAuthorName = item.CorrespondingAuthorName,
            otherAuthorsText = item.OtherAuthorsText,
            authorCount = item.AuthorCount,
            paymentStatusId = item.PaymentStatusId,
            paymentStatusCode = item.PaymentStatusCode,
            paymentStatusName = item.PaymentStatusName,
            paymentStatusBadgeClass = item.PaymentStatusBadgeClass,
            transactionStatusId = item.TransactionStatusId,
            transactionStatusCode = item.TransactionStatusCode,
            transactionStatusName = item.TransactionStatusName,
            transactionStatusBadgeClass = item.TransactionStatusBadgeClass,
            displayDate = FormatDate(displayDate),
            displayTime = FormatTime(displayDate),
            canEdit = item.CanEdit,
            canDelete = item.CanDelete
        };
    }

    private static object BuildStats(IEnumerable<GetListSubmissionListItemDto>? items)
    {
        List<GetListSubmissionListItemDto> rows = items?.ToList() ?? new List<GetListSubmissionListItemDto>();

        return new
        {
            total = rows.Count,
            submitted = rows.Count(IsSubmitted),
            reviewerProcess = rows.Count(IsReviewerProcess),
            accepted = rows.Count(IsAccepted),
            rejected = rows.Count(IsRejected),
            paymentPending = rows.Count(IsPaymentPending),
            paymentCompleted = rows.Count(IsPaymentCompleted)
        };
    }

    private static bool IsSubmitted(GetListSubmissionListItemDto item)
        => IsStatusCode(item.TransactionStatusCode, "SUBMITTED") || item.TransactionStatusId == 110 || ContainsAny(item.TransactionStatusName, "gönderildi", "submitted");

    private static bool IsReviewerProcess(GetListSubmissionListItemDto item)
        => item.TransactionStatusId is 130 or 140
            || IsStatusCode(item.TransactionStatusCode, "REVIEWER_ASSIGNMENT", "UNDER_REVIEW")
            || ContainsAny(item.TransactionStatusName, "hakem", "review");

    private static bool IsAccepted(GetListSubmissionListItemDto item)
        => IsStatusCode(item.TransactionStatusCode, "ACCEPTED") || item.TransactionStatusId == 180 || ContainsAny(item.TransactionStatusName, "kabul", "accepted");

    private static bool IsRejected(GetListSubmissionListItemDto item)
        => IsStatusCode(item.TransactionStatusCode, "REJECTED") || item.TransactionStatusId == 190 || ContainsAny(item.TransactionStatusName, "red", "reject");

    private static bool IsPaymentPending(GetListSubmissionListItemDto item)
    {
        string statusCode = NormalizeCode(item.PaymentStatusCode);
        if (statusCode is "PAYMENTPENDING" or "PENDING" or "WAITING" or "WAITINGPAYMENT")
            return true;

        string statusName = NormalizeCode(item.PaymentStatusName);
        return statusName.Contains("BEKLIYOR", StringComparison.Ordinal) ||
               statusName.Contains("PENDING", StringComparison.Ordinal) ||
               statusName.Contains("WAITING", StringComparison.Ordinal);
    }

    private static bool IsPaymentCompleted(GetListSubmissionListItemDto item)
    {
        string statusCode = NormalizeCode(item.PaymentStatusCode);
        if (statusCode is "PAYMENTCOMPLETED" or "COMPLETED" or "PAID" or "PAYMENTPAID" or "PAYMENTDONE" or "APPROVED" or "PAYMENTAPPROVED")
            return true;

        string statusName = NormalizeCode(item.PaymentStatusName);
        return statusName.Contains("ODEMEYAPILDI", StringComparison.Ordinal) ||
               statusName.Contains("ODEMEISLEMIYAPILDI", StringComparison.Ordinal) ||
               statusName.Contains("ODEMEALINDI", StringComparison.Ordinal) ||
               statusName.Contains("ONAYLANDI", StringComparison.Ordinal) ||
               statusName.Contains("TAMAMLANDI", StringComparison.Ordinal) ||
               statusName.Contains("COMPLETED", StringComparison.Ordinal) ||
               statusName.Contains("PAID", StringComparison.Ordinal) ||
               statusName.Contains("APPROVED", StringComparison.Ordinal);
    }

    private static bool IsStatusCode(string? value, params string[] expectedCodes)
    {
        string normalized = NormalizeCode(value);
        return expectedCodes.Any(expected => string.Equals(normalized, NormalizeCode(expected), StringComparison.Ordinal));
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

    private static bool ContainsAny(string? value, params string[] tokens)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return tokens.Any(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static string FormatDate(DateTime? value)
        => IsMeaningfulDate(value) ? value!.Value.ToString("dd.MM.yyyy") : "-";

    private static string FormatTime(DateTime? value)
        => IsMeaningfulDate(value) ? value!.Value.ToString("HH:mm") : "-";

    private static bool IsMeaningfulDate(DateTime? value)
        => value.HasValue && value.Value.Year >= 1900;

    private IActionResult RedirectToLocalReturnUrlOrIndex(string? returnUrl)
    {
        string? normalizedReturnUrl = NormalizeLocalReturnUrl(returnUrl);
        if (!string.IsNullOrWhiteSpace(normalizedReturnUrl))
            return LocalRedirect(normalizedReturnUrl);

        return RedirectToIndex();
    }

    private string? NormalizeLocalReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return null;

        return Url.IsLocalUrl(returnUrl) ? returnUrl : null;
    }

    private RedirectToActionResult RedirectToIndex()
    {
        string? culture = RouteData.Values["culture"]?.ToString();
        return string.IsNullOrWhiteSpace(culture)
            ? RedirectToAction(nameof(Index))
            : RedirectToAction(nameof(Index), new { culture });
    }

    private bool CanCurrentUserManageAllSubmissions()
    {
        return User.IsInRole("SuperAdmin") ||
               User.IsInRole("CongressEditor") ||
               HasPermission("Submissions.Admin");
    }

    private bool HasPermission(string permission)
    {
        return User.Claims.Any(claim =>
            (string.Equals(claim.Type, "Permission", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(claim.Type, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase)) &&
            string.Equals(claim.Value, permission, StringComparison.OrdinalIgnoreCase));
    }

    private static Guid? NormalizeGuid(Guid? value)
    {
        return value.HasValue && value.Value != Guid.Empty ? value : null;
    }

    private static int? NormalizeTransactionStatusId(int? value)
    {
        return value.HasValue && value.Value > 0 ? value : null;
    }

    private static int? NormalizePaymentStatusId(int? value)
    {
        return value.HasValue && value.Value > 0 ? value : null;
    }

    private static SubmissionOwnerMultiplicityFilter NormalizeOwnerMultiplicity(int? value)
    {
        return value switch
        {
            (int)SubmissionOwnerMultiplicityFilter.Single => SubmissionOwnerMultiplicityFilter.Single,
            (int)SubmissionOwnerMultiplicityFilter.Multiple => SubmissionOwnerMultiplicityFilter.Multiple,
            _ => SubmissionOwnerMultiplicityFilter.All
        };
    }

    private static string? NormalizeSearchText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private Guid? GetCurrentUserId()
    {
        string? rawId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(rawId, out Guid userId) ? userId : null;
    }

    private sealed record SubmissionManagementFileUploadInput(
        string OriginalFileName,
        string FilePath,
        string? ContentType,
        long? FileSize);
}
