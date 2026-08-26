using System.Security.Claims;
using Core.Application.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Features.UserProfiles.Constants;
using Symplify.BackOffice.Application.Common.Text;
using Symplify.BackOffice.Domain.Identity;
using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.WebUI.Models.Profile;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture?}/profile")]
public sealed class ProfileController : Controller
{
    private const long MaxProfileImageSizeInBytes = 5 * 1024 * 1024;
    private const string ProfileImageRootSegment = "profile-photos";

    private static readonly HashSet<string> AllowedProfileImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private static readonly HashSet<string> AllowedProfileImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private readonly UserManager<AppUser> _userManager;
    private readonly IBackOfficeResourceProvider _resourceProvider;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ObjectStorageOptions _storageOptions;

    public ProfileController(
        UserManager<AppUser> userManager,
        IBackOfficeResourceProvider resourceProvider,
        IObjectStorageService objectStorageService,
        IOptions<ObjectStorageOptions> storageOptions)
    {
        _userManager = userManager;
        _resourceProvider = resourceProvider;
        _objectStorageService = objectStorageService;
        _storageOptions = storageOptions.Value;
    }

    [HttpGet("complete-phone")]
    public async Task<IActionResult> CompletePhone(string? returnUrl = null)
    {
        AppUser? user = await GetCurrentUserAsync();

        if (user is null)
            return RedirectToAction("Login", "Auth", new { culture = GetCurrentCulture() });

        if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
            return RedirectToSafeReturnUrl(returnUrl);

        return View(new CompletePhoneViewModel
        {
            ReturnUrl = returnUrl
        });
    }

    [HttpPost("complete-phone")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompletePhone(CompletePhoneViewModel model)
    {
        AppUser? user = await GetCurrentUserAsync();

        if (user is null)
            return RedirectToAction("Login", "Auth", new { culture = GetCurrentCulture() });

        string normalizedPhone = NormalizePhoneNumber(model.PhoneNumber);
        if (string.IsNullOrWhiteSpace(normalizedPhone) || !IsValidE164PhoneNumber(normalizedPhone))
        {
            ModelState.AddModelError(nameof(model.PhoneNumber), T(ProfileResourceKeys.CompletePhoneValidationInvalid));
        }

        if (!ModelState.IsValid)
            return View(model);

        user.PhoneNumber = normalizedPhone;
        user.PhoneNumberConfirmed = false;
        user.UpdatedDate = DateTime.UtcNow;
        user.UpdatedBy = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;

        IdentityResult result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View(model);
        }

        TempData["SuccessMessage"] = T(ProfileResourceKeys.CompletePhoneSuccess);

        return RedirectToSafeReturnUrl(model.ReturnUrl);
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        AppUser? user = await GetCurrentUserAsync();

        if (user is null)
            return RedirectToAction("Login", "Auth", new { culture = GetCurrentCulture() });

        return View(await BuildProfileViewModelAsync(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([Bind(Prefix = "Profile")] ProfileUpdateViewModel model, CancellationToken cancellationToken)
    {
        AppUser? user = await GetCurrentUserAsync();

        if (user is null)
            return RedirectToAction("Login", "Auth", new { culture = GetCurrentCulture() });

        string normalizedPhone = NormalizePhoneNumber(model.PhoneNumber);
        if (string.IsNullOrWhiteSpace(normalizedPhone) || !IsValidE164PhoneNumber(normalizedPhone))
        {
            ModelState.AddModelError("Profile.PhoneNumber", T(ProfileResourceKeys.ValidationPhoneInvalid));
        }

        if (model.ProfileImageFile is not null && model.ProfileImageFile.Length > 0)
        {
            ValidateProfileImage(model.ProfileImageFile);
        }

        if (!ModelState.IsValid)
        {
            ProfileViewModel viewModel = await BuildProfileViewModelAsync(user);
            viewModel.Profile = model;
            viewModel.Profile.ProfileImageUrl = BuildProfileImageUrl(user.ProfileImageObjectName);
            return View("Index", viewModel);
        }

        user.Name = BackOfficeTextNormalizer.NormalizeRequiredPersonFirstName(model.Name);
        user.Surname = BackOfficeTextNormalizer.NormalizeRequiredPersonSurname(model.Surname);
        user.PhoneNumber = normalizedPhone;
        user.Institution = BackOfficeTextNormalizer.NormalizeInstitution(model.Institution);
        user.Orcid = string.IsNullOrWhiteSpace(model.Orcid) ? null : model.Orcid.Trim();

        if (model.ProfileImageFile is not null && model.ProfileImageFile.Length > 0)
        {
            try
            {
                string? previousObjectName = user.ProfileImageObjectName;
                user.ProfileImageObjectName = await UploadProfileImageAsync(user.Id, model.ProfileImageFile, cancellationToken);
                await DeletePreviousProfileImageAsync(previousObjectName, user.ProfileImageObjectName, cancellationToken);
            }
            catch (InvalidOperationException exception)
            {
                ModelState.AddModelError("Profile.ProfileImageFile", string.IsNullOrWhiteSpace(exception.Message)
                    ? T(ProfileResourceKeys.StorageConfigurationInvalid)
                    : exception.Message);

                ProfileViewModel viewModel = await BuildProfileViewModelAsync(user);
                viewModel.Profile = model;
                viewModel.Profile.ProfileImageUrl = BuildProfileImageUrl(user.ProfileImageObjectName);
                return View("Index", viewModel);
            }
        }

        user.UpdatedDate = DateTime.UtcNow;
        user.UpdatedBy = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;

        IdentityResult result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            ProfileViewModel viewModel = await BuildProfileViewModelAsync(user);
            viewModel.Profile = model;
            viewModel.Profile.ProfileImageUrl = BuildProfileImageUrl(user.ProfileImageObjectName);
            return View("Index", viewModel);
        }

        TempData["SuccessMessage"] = T(ProfileResourceKeys.SuccessUpdated);

        return RedirectToAction(nameof(Index), new { culture = GetCurrentCulture() });
    }

    [HttpPost("change-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([Bind(Prefix = "ChangePassword")] ChangePasswordViewModel model, CancellationToken cancellationToken)
    {
        AppUser? user = await GetCurrentUserAsync();

        if (user is null)
            return RedirectToAction("Login", "Auth", new { culture = GetCurrentCulture() });

        if (!ModelState.IsValid)
        {
            ProfileViewModel viewModel = await BuildProfileViewModelAsync(user);
            viewModel.ChangePassword = model;
            return View("Index", viewModel);
        }

        IdentityResult result = await _userManager.ChangePasswordAsync(
            user,
            model.CurrentPassword,
            model.NewPassword);

        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            ProfileViewModel viewModel = await BuildProfileViewModelAsync(user);
            viewModel.ChangePassword = model;
            return View("Index", viewModel);
        }

        await _userManager.UpdateSecurityStampAsync(user);

        TempData["SuccessMessage"] = T(ProfileResourceKeys.SuccessPasswordChanged);

        return RedirectToAction(nameof(Index), new { culture = GetCurrentCulture() });
    }

    private async Task<ProfileViewModel> BuildProfileViewModelAsync(AppUser user)
    {
        IList<string> roles = await _userManager.GetRolesAsync(user);

        return new ProfileViewModel
        {
            Profile = new ProfileUpdateViewModel
            {
                Id = user.Id,
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                PhoneNumberDisplay = GetNationalPhoneDisplayValue(user.PhoneNumber),
                Institution = user.Institution,
                Orcid = user.Orcid,
                ProfileImageUrl = BuildProfileImageUrl(user.ProfileImageObjectName)
            },
            Roles = roles.ToArray()
        };
    }

    private Task<AppUser?> GetCurrentUserAsync()
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Task.FromResult<AppUser?>(null);

        return _userManager.FindByIdAsync(userId);
    }

    private void ValidateProfileImage(IFormFile file)
    {
        if (file.Length > MaxProfileImageSizeInBytes)
        {
            ModelState.AddModelError("Profile.ProfileImageFile", T(ProfileResourceKeys.ValidationImageTooLarge));
            return;
        }

        string extension = Path.GetExtension(file.FileName);
        string contentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? string.Empty
            : file.ContentType.Trim();

        if (string.IsNullOrWhiteSpace(extension) ||
            !AllowedProfileImageExtensions.Contains(extension) ||
            !AllowedProfileImageContentTypes.Contains(contentType))
        {
            ModelState.AddModelError("Profile.ProfileImageFile", T(ProfileResourceKeys.ValidationImageInvalid));
        }
    }

    private async Task<string> UploadProfileImageAsync(Guid userId, IFormFile file, CancellationToken cancellationToken)
    {
        string bucketName = GetProfileImageBucketName();
        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        string fileName = $"profile-{Guid.NewGuid():N}{extension}";
        string objectName = string.Join(
            '/',
            "backoffice",
            ProfileImageRootSegment,
            userId.ToString("N"),
            DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"),
            fileName);

        await using Stream stream = file.OpenReadStream();

        await _objectStorageService.UploadAsync(
            new ObjectStorageUploadRequest
            {
                BucketName = bucketName,
                ObjectName = objectName,
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                Size = file.Length,
                Content = stream,
                Metadata = new Dictionary<string, string>
                {
                    ["feature"] = "profile",
                    ["user-id"] = userId.ToString("N")
                }
            },
            cancellationToken);

        return objectName;
    }

    private async Task DeletePreviousProfileImageAsync(string? previousObjectName, string? currentObjectName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(previousObjectName) ||
            string.Equals(previousObjectName, currentObjectName, StringComparison.Ordinal))
        {
            return;
        }

        if (!previousObjectName.Replace('\\', '/').Contains($"/{ProfileImageRootSegment}/", StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            await _objectStorageService.DeleteAsync(
                new ObjectStorageDeleteRequest
                {
                    BucketName = GetProfileImageBucketName(),
                    ObjectName = previousObjectName
                },
                cancellationToken);
        }
        catch
        {
            // Best effort cleanup. The new DB state remains authoritative.
        }
    }

    private string GetProfileImageBucketName()
    {
        string? bucketName = _storageOptions.Buckets.CongressImages;

        if (string.IsNullOrWhiteSpace(bucketName))
            throw new InvalidOperationException(T(ProfileResourceKeys.StorageConfigurationInvalid));

        return bucketName.Trim();
    }

    private string? BuildProfileImageUrl(string? objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName) || string.IsNullOrWhiteSpace(_storageOptions.Buckets.CongressImages))
            return null;

        string bucketName = _storageOptions.Buckets.CongressImages.Trim();
        string encodedBucketName = Uri.EscapeDataString(bucketName);
        string encodedObjectName = string.Join(
            '/',
            objectName
                .Trim()
                .TrimStart('/')
                .Replace('\\', '/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Uri.EscapeDataString));

        return $"/public-assets/{encodedBucketName}/{encodedObjectName}";
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (IdentityError error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    private IActionResult RedirectToSafeReturnUrl(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction("Index", "Home", new { culture = GetCurrentCulture() });
    }

    private static string GetNationalPhoneDisplayValue(string? value)
    {
        string normalized = NormalizePhoneNumber(value);
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        if (normalized.StartsWith("+90", StringComparison.Ordinal) && normalized.Length > 3)
            return normalized[3..];

        return normalized.TrimStart('+');
    }

    private static string NormalizePhoneNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string trimmed = value.Trim();
        if (trimmed.StartsWith("00", StringComparison.Ordinal))
            trimmed = "+" + trimmed[2..];

        string normalized = new(trimmed
            .Where((character, index) => char.IsDigit(character) || (character == '+' && index == 0))
            .ToArray());

        return normalized;
    }

    private static bool IsValidE164PhoneNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (!value.StartsWith('+'))
            return false;

        int digitCount = value.Count(char.IsDigit);
        return digitCount is >= 8 and <= 15;
    }

    private string T(string key)
    {
        string value = _resourceProvider.GetStringValue(key);
        return string.IsNullOrWhiteSpace(value) ? key : value;
    }

    private string GetCurrentCulture()
    {
        string? routeCulture = RouteData.Values["culture"]?.ToString();
        return string.IsNullOrWhiteSpace(routeCulture) ? "tr-TR" : routeCulture;
    }
}
