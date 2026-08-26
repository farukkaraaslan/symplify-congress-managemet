using System.Globalization;
using System.Security.Claims;
using Core.Application.Storage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Common.Storage;
using Symplify.BackOffice.Application.Common.Text;
using Symplify.BackOffice.Application.Features.ExhibitionApplications.Commands.Create;
using Symplify.BackOffice.Application.Features.Submissions.Commands.Create;
using Symplify.BackOffice.Application.Features.Submissions.Queries.GetCreatePage;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Identity;
using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.WebUI.Models.ExhibitionApplications;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture?}/exhibition-applications")]
public sealed class ExhibitionApplicationsController : Controller
{
    private static readonly HashSet<string> AllowedFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".pdf"
    };

    private const long MaxFileSize = 20 * 1024 * 1024;

    private readonly IMediator _mediator;
    private readonly UserManager<AppUser> _userManager;
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly ICongressRepository _congressRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ObjectStorageOptions _objectStorageOptions;
    private readonly IBackOfficeViewLocalizer _localizer;

    public ExhibitionApplicationsController(
        IMediator mediator,
        UserManager<AppUser> userManager,
        IOrganizationUserRepository organizationUserRepository,
        ICongressRepository congressRepository,
        IObjectStorageService objectStorageService,
        IOptions<ObjectStorageOptions> objectStorageOptions,
        IBackOfficeViewLocalizer localizer)
    {
        _mediator = mediator;
        _userManager = userManager;
        _organizationUserRepository = organizationUserRepository;
        _congressRepository = congressRepository;
        _objectStorageService = objectStorageService;
        _objectStorageOptions = objectStorageOptions.Value;
        _localizer = localizer;
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(Guid? submissionTypeId, CancellationToken cancellationToken)
    {
        CreateExhibitionApplicationViewModel model = await BuildCreateViewModelAsync(
            new CreateExhibitionApplicationViewModel
            {
                SubmissionTypeId = NormalizeOptionalGuid(submissionTypeId)
            },
            cancellationToken);

        if (model.SubmissionTypeId.HasValue && string.IsNullOrWhiteSpace(model.SubmissionTypeName))
            return NotFound();

        return View(model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<IActionResult> Create(CreateExhibitionApplicationViewModel model, CancellationToken cancellationToken)
    {
        SubmissionCongressContext? submissionCongress = await ResolveCurrentUserSubmissionCongressAsync(cancellationToken);
        ModelState.Remove(nameof(model.CongressId));
        ModelState.Remove(nameof(model.CongressName));
        ModelState.Remove(nameof(model.SubmissionTypeId));
        ModelState.Remove(nameof(model.SubmissionTypeName));
        RemoveClientGeneratedValidationMessages();
        NormalizeInput(model);

        if (submissionCongress is null)
        {
            model.CongressId = Guid.Empty;
            ModelState.AddModelError(nameof(model.CongressId), GetText(
                "BackOffice.ExhibitionApplications.Create.Validation.NoActiveCongress",
                "Sergi başvurusu gönderebileceğiniz aktif bir kongre üyeliği bulunamadı.",
                "No active congress membership was found for exhibition application."));
        }
        else
        {
            model.CongressId = submissionCongress.Id;
            model.CongressName = submissionCongress.Name;
        }

        model = await BuildCreateViewModelAsync(model, cancellationToken);

        if (!model.SubmissionTypeId.HasValue || string.IsNullOrWhiteSpace(model.SubmissionTypeName))
            ModelState.AddModelError(nameof(model.SubmissionTypeId), GetText(
                "BackOffice.ExhibitionApplications.Create.Validation.TypeRequired",
                "Sergi başvurusu için geçerli bir başvuru türü seçilmelidir.",
                "A valid exhibition application type must be selected."));

        ValidateTextFields(model);
        ValidateFile(model.ExhibitionFile);

        if (!ModelState.IsValid)
            return View(model);

        string? uploadedObjectName = null;

        try
        {
            ExhibitionApplicationFileInputDto fileInput = await UploadFileAsync(model, cancellationToken);
            uploadedObjectName = fileInput.FilePath;

            AppUser? currentUser = await _userManager.GetUserAsync(User);
            string? email = currentUser?.Email ?? User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
            string fullName = BuildCurrentUserFullName(currentUser, email);

            await _mediator.Send(new CreateExhibitionApplicationCommand
            {
                CongressId = model.CongressId,
                SubmissionTypeId = model.SubmissionTypeId,
                CreatedByUserId = GetCurrentUserId(),
                WorkName = model.WorkName,
                Dimensions = model.Dimensions,
                Technique = model.Technique,
                Description = model.Description,
                Address = model.Address,
                File = fileInput,
                Authors = new List<SubmissionAuthorInputDto>
                {
                    new()
                    {
                        TitleId = currentUser?.TitleId,
                        FirstName = BackOfficeTextNormalizer.NormalizePersonFirstName(currentUser?.Name),
                        LastName = BackOfficeTextNormalizer.NormalizePersonSurname(currentUser?.Surname),
                        FullName = fullName,
                        Email = email,
                        Institution = currentUser?.Institution,
                        Orcid = currentUser?.Orcid,
                        IsCorrespondingAuthor = true
                    }
                }
            }, cancellationToken);

            TempData["SuccessMessage"] = GetText(
                "BackOffice.ExhibitionApplications.Create.Success",
                "Sergi başvurusu oluşturuldu ve onaya gönderildi.",
                "The exhibition application has been created and submitted for review.");
            return RedirectToAction("Index", "Submissions", new { culture = RouteData.Values["culture"]?.ToString() });
        }
        catch
        {
            await BackOfficeObjectStorageHelper.DeleteObjectIfExistsAsync(
                _objectStorageService,
                ResolveSubmissionBucketName(),
                uploadedObjectName,
                cancellationToken);

            throw;
        }
    }

    private async Task<CreateExhibitionApplicationViewModel> BuildCreateViewModelAsync(
        CreateExhibitionApplicationViewModel model,
        CancellationToken cancellationToken)
    {
        SubmissionCongressContext? submissionCongress = await ResolveCurrentUserSubmissionCongressAsync(cancellationToken);

        if (submissionCongress is null)
        {
            model.CongressId = Guid.Empty;
            model.CongressName = string.Empty;
            model.SubmissionTypeName = string.Empty;
            return model;
        }

        model.CongressId = submissionCongress.Id;

        GetSubmissionCreatePageResponse createPage = await _mediator.Send(new GetSubmissionCreatePageQuery
        {
            CongressId = submissionCongress.Id,
            Culture = RouteData.Values["culture"]?.ToString()
        }, cancellationToken);

        model.CongressName = createPage.Congresses
            .FirstOrDefault(item => item.Id == submissionCongress.Id)?.Text
            ?? submissionCongress.Name;

        IReadOnlyList<SubmissionCreateSelectItemDto> exhibitionTypes = createPage.SubmissionTypes
            .Where(item => item.FormProfile == SubmissionFormProfile.ExhibitionApplication)
            .ToList();

        if (!model.SubmissionTypeId.HasValue || model.SubmissionTypeId.Value == Guid.Empty)
            model.SubmissionTypeId = exhibitionTypes.FirstOrDefault()?.Id;

        SubmissionCreateSelectItemDto? selectedType = exhibitionTypes
            .FirstOrDefault(item => item.Id == model.SubmissionTypeId);

        model.SubmissionTypeName = selectedType?.Text ?? string.Empty;

        return model;
    }

    private async Task<ExhibitionApplicationFileInputDto> UploadFileAsync(CreateExhibitionApplicationViewModel model, CancellationToken cancellationToken)
    {
        IFormFile file = model.ExhibitionFile ?? throw new InvalidOperationException(GetText(
            "BackOffice.ExhibitionApplications.Create.Validation.FileRequired",
            "Sergi görseli yüklenmelidir.",
            "The exhibition file is required."));
        string? bucketName = ResolveSubmissionBucketName();

        if (string.IsNullOrWhiteSpace(bucketName))
            throw new InvalidOperationException(GetText(
                "BackOffice.ExhibitionApplications.Create.Validation.StorageBucketMissing",
                "Başvuru dosya depolama ayarı yapılandırılmamış.",
                "Submission file storage bucket is not configured."));

        string extension = Path.GetExtension(file.FileName);
        string safeOriginalFileName = string.IsNullOrWhiteSpace(file.FileName)
            ? $"exhibition-file{extension}"
            : Path.GetFileName(file.FileName);

        string objectName = BackOfficeObjectStorageHelper.BuildObjectName(
            "submissions",
            "exhibition-applications",
            Guid.NewGuid().ToString("N"),
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
                    ["module"] = "submission-exhibition-application",
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

    private void ValidateFile(IFormFile? file)
    {
        if (file is null || file.Length <= 0)
        {
            ModelState.AddModelError(nameof(CreateExhibitionApplicationViewModel.ExhibitionFile), GetText(
                "BackOffice.ExhibitionApplications.Create.Validation.FileRequired",
                "Sergi görseli yüklenmelidir.",
                "The exhibition file is required."));
            return;
        }

        if (file.Length > MaxFileSize)
            ModelState.AddModelError(nameof(CreateExhibitionApplicationViewModel.ExhibitionFile), GetText(
                "BackOffice.ExhibitionApplications.Create.Validation.FileTooLarge",
                "Sergi dosyası en fazla 20 MB olabilir.",
                "The exhibition file can be at most 20 MB."));

        string extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedFileExtensions.Contains(extension))
            ModelState.AddModelError(nameof(CreateExhibitionApplicationViewModel.ExhibitionFile), GetText(
                "BackOffice.ExhibitionApplications.Create.Validation.FileInvalidType",
                "Sergi dosyası JPG, PNG, WEBP veya PDF formatında olmalıdır.",
                "The exhibition file must be in JPG, PNG, WEBP, or PDF format."));
    }

    private void NormalizeInput(CreateExhibitionApplicationViewModel model)
    {
        model.WorkName = BackOfficeTextNormalizer.NormalizeRequiredSubmissionTitleTr(model.WorkName);
        model.Dimensions = string.IsNullOrWhiteSpace(model.Dimensions) ? null : model.Dimensions.Trim();
        model.Technique = (model.Technique ?? string.Empty).Trim();
        model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        model.Address = (model.Address ?? string.Empty).Trim();
    }

    private void RemoveClientGeneratedValidationMessages()
    {
        ModelState.Remove(nameof(CreateExhibitionApplicationViewModel.WorkName));
        ModelState.Remove(nameof(CreateExhibitionApplicationViewModel.Dimensions));
        ModelState.Remove(nameof(CreateExhibitionApplicationViewModel.Technique));
        ModelState.Remove(nameof(CreateExhibitionApplicationViewModel.Description));
        ModelState.Remove(nameof(CreateExhibitionApplicationViewModel.Address));
        ModelState.Remove(nameof(CreateExhibitionApplicationViewModel.ExhibitionFile));
    }

    private void ValidateTextFields(CreateExhibitionApplicationViewModel model)
    {
        ValidateRequiredText(
            nameof(CreateExhibitionApplicationViewModel.WorkName),
            model.WorkName,
            300,
            "BackOffice.ExhibitionApplications.Create.Validation.WorkNameRequired",
            "Eser adı zorunludur.",
            "Artwork name is required.",
            "BackOffice.ExhibitionApplications.Create.Validation.WorkNameMaxLength",
            "Eser adı en fazla 300 karakter olabilir.",
            "Artwork name can be at most 300 characters.");

        ValidateOptionalText(
            nameof(CreateExhibitionApplicationViewModel.Dimensions),
            model.Dimensions,
            200,
            "BackOffice.ExhibitionApplications.Create.Validation.DimensionsMaxLength",
            "Eser ölçüleri en fazla 200 karakter olabilir.",
            "Artwork dimensions can be at most 200 characters.");

        ValidateRequiredText(
            nameof(CreateExhibitionApplicationViewModel.Technique),
            model.Technique,
            250,
            "BackOffice.ExhibitionApplications.Create.Validation.TechniqueRequired",
            "Uygulanan teknik zorunludur.",
            "Applied technique is required.",
            "BackOffice.ExhibitionApplications.Create.Validation.TechniqueMaxLength",
            "Uygulanan teknik en fazla 250 karakter olabilir.",
            "Applied technique can be at most 250 characters.");

        ValidateOptionalText(
            nameof(CreateExhibitionApplicationViewModel.Description),
            model.Description,
            4000,
            "BackOffice.ExhibitionApplications.Create.Validation.DescriptionMaxLength",
            "Açıklama en fazla 4000 karakter olabilir.",
            "Description can be at most 4000 characters.");

        ValidateRequiredText(
            nameof(CreateExhibitionApplicationViewModel.Address),
            model.Address,
            1000,
            "BackOffice.ExhibitionApplications.Create.Validation.AddressRequired",
            "Adres zorunludur.",
            "Address is required.",
            "BackOffice.ExhibitionApplications.Create.Validation.AddressMaxLength",
            "Adres en fazla 1000 karakter olabilir.",
            "Address can be at most 1000 characters.");
    }

    private void ValidateRequiredText(
        string fieldName,
        string? value,
        int maxLength,
        string requiredKey,
        string requiredTr,
        string requiredEn,
        string maxLengthKey,
        string maxLengthTr,
        string maxLengthEn)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            ModelState.AddModelError(fieldName, GetText(requiredKey, requiredTr, requiredEn));
            return;
        }

        ValidateOptionalText(fieldName, value, maxLength, maxLengthKey, maxLengthTr, maxLengthEn);
    }

    private void ValidateOptionalText(
        string fieldName,
        string? value,
        int maxLength,
        string maxLengthKey,
        string maxLengthTr,
        string maxLengthEn)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Length > maxLength)
            ModelState.AddModelError(fieldName, GetText(maxLengthKey, maxLengthTr, maxLengthEn));
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

    private string? ResolveSubmissionBucketName()
        => string.IsNullOrWhiteSpace(_objectStorageOptions.Buckets.Submissions)
            ? null
            : _objectStorageOptions.Buckets.Submissions.Trim();

    private Guid? GetCurrentOrganizationId()
    {
        string? organizationId = User.FindFirstValue("OrganizationId");
        return Guid.TryParse(organizationId, out Guid parsedOrganizationId)
            ? parsedOrganizationId
            : null;
    }

    private Guid? GetCurrentUserId()
    {
        string? rawId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(rawId, out Guid userId) ? userId : null;
    }

    private string GetText(string key, string trFallback, string? enFallback = null)
    {
        string value = _localizer.GetStringValue(key);

        if (!string.IsNullOrWhiteSpace(value) &&
            !string.Equals(value, key, StringComparison.OrdinalIgnoreCase))
            return value;

        string culture = RouteData.Values["culture"]?.ToString()
                         ?? CultureInfo.CurrentUICulture.Name;

        return culture.StartsWith("en", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(enFallback)
            ? enFallback
            : trFallback;
    }

    private static Guid? NormalizeOptionalGuid(Guid? value)
        => value.HasValue && value.Value != Guid.Empty ? value.Value : null;

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

        return string.IsNullOrWhiteSpace(fallbackEmail) ? "Kullanıcı" : fallbackEmail.Trim();
    }

    private sealed record SubmissionCongressContext(Guid Id, string Name);
}
