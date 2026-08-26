using System.Net.Mail;
using System.Text.RegularExpressions;
using Core.Application.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.Application.Features.Organizations.Commands.Create;
using Symplify.BackOffice.Application.Features.Organizations.Commands.Delete;
using Symplify.BackOffice.Application.Features.Organizations.Commands.Update;
using Symplify.BackOffice.Application.Features.Organizations.Queries.GetById;
using Symplify.BackOffice.Application.Features.Organizations.Queries.GetList;
using Symplify.BackOffice.WebUI.Extensions;
using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.WebUI.Models.Organizations;
using Symplify.BackOffice.WebUI.Models.Shared.DataTables;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture=tr-TR}/[controller]/[action]")]
public sealed class OrganizationsController : Controller
{
    private static readonly Regex OrganizationCodeRegex = new("^[a-zA-Z0-9-]+$", RegexOptions.Compiled);

    private readonly IMediator _mediator;
    private readonly IBackOfficeViewLocalizer _localizer;

    public OrganizationsController(
        IMediator mediator,
        IBackOfficeViewLocalizer localizer)
    {
        _mediator = mediator;
        _localizer = localizer;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new OrganizationsIndexViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GetList(
        [FromForm] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        DataTableQueryOptions options = DataTableQueryOptions.From(
            request,
            "name",
            "asc",
            new[] { "name", "code", "brandColor", "isActive", "updatedDate" });

        var response = await _mediator.Send(
            new GetListOrganizationQuery
            {
                SearchText = options.SearchText,
                SortColumn = options.SortColumn,
                SortDirection = options.SortDirection,
                PageRequest = new PageRequest
                {
                    Page = options.Page,
                    PageSize = options.PageSize
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
                rowNumber = options.Start + index + 1,
                id = item.Id,
                name = item.Name,
                code = item.Code,
                shortName = item.ShortName,
                logoPath = item.LogoLightUrl ?? item.LogoLightPath,
                logoLightPath = item.LogoLightUrl ?? item.LogoLightPath,
                logoDarkPath = item.LogoDarkUrl ?? item.LogoDarkPath,
                logoLightObjectPath = item.LogoLightPath,
                logoDarkObjectPath = item.LogoDarkPath,
                brandColor = NormalizeBrandColor(item.BrandColor) ?? "#487FFF",
                isActive = item.IsActive,
                activeApiKeyCount = item.ActiveApiKeyCount,
                lastUpdatedAt = FormatDate(item.UpdatedDate ?? item.CreatedDate)
            })
        });
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateOrganizationViewModel
        {
            IsActive = true,
            BrandColor = "#487FFF"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromForm] CreateOrganizationViewModel model,
        CancellationToken cancellationToken)
    {
        NormalizeOrganizationModelState(model);

        if (!ModelState.IsValid)
            return BadRequest(CreateValidationErrorResponse());

        try
        {
            await _mediator.Send(
                new CreateOrganizationCommand
                {
                    Name = NormalizeText(model.Name)!,
                    Code = NormalizeCode(model.Code!),
                    ShortName = NormalizeText(model.ShortName),
                    WebsiteUrl = NormalizeText(model.WebsiteUrl),
                    Description = NormalizeText(model.Description),
                    ContactName = NormalizeText(model.ContactName),
                    ContactTitle = NormalizeText(model.ContactTitle),
                    ContactEmail = NormalizeText(model.ContactEmail),
                    ContactPhone = NormalizeText(model.ContactPhone),
                    ContactNote = NormalizeText(model.ContactNote),
                    BrandColor = GetPostedBrandColor(model.BrandColor),
                    LogoLight = model.LogoLightFile.ToOrganizationLogoInputDto(),
                    LogoDark = model.LogoDarkFile.ToOrganizationLogoInputDto(),
                    IsActive = model.IsActive
                },
                cancellationToken);

            string successMessage = GetText("BackOffice.Organizations.Messages.Created", "Organizasyon başarıyla oluşturuldu.");
            string redirectUrl = Url.Action(nameof(Index), "Organizations", new { culture = CurrentCulture() })
                ?? $"/{CurrentCulture()}/organizations/index";

            return Json(new
            {
                success = true,
                message = successMessage,
                redirectUrl
            });
        }
        catch (Exception exception)
        {
            string message = GetExceptionMessage(exception);

            return BadRequest(new
            {
                success = false,
                message
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest();

        UpdateOrganizationViewModel model = await BuildUpdateViewModelAsync(id, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        [FromForm] UpdateOrganizationViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.Id == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(model.Id),
                GetText("Common.InvalidRequest", "Geçersiz istek."));
        }

        NormalizeOrganizationModelState(model);

        if (!ModelState.IsValid)
            return BadRequest(CreateValidationErrorResponse());

        try
        {
            await _mediator.Send(
                new UpdateOrganizationCommand
                {
                    Id = model.Id,
                    Name = NormalizeText(model.Name)!,
                    Code = NormalizeCode(model.Code!),
                    ShortName = NormalizeText(model.ShortName),
                    WebsiteUrl = NormalizeText(model.WebsiteUrl),
                    Description = NormalizeText(model.Description),
                    ContactName = NormalizeText(model.ContactName),
                    ContactTitle = NormalizeText(model.ContactTitle),
                    ContactEmail = NormalizeText(model.ContactEmail),
                    ContactPhone = NormalizeText(model.ContactPhone),
                    ContactNote = NormalizeText(model.ContactNote),
                    BrandColor = GetPostedBrandColor(model.BrandColor),
                    LogoLightPath = model.ExistingLogoLightPath,
                    LogoDarkPath = model.ExistingLogoDarkPath,
                    LogoLight = model.LogoLightFile.ToOrganizationLogoInputDto(),
                    LogoDark = model.LogoDarkFile.ToOrganizationLogoInputDto(),
                    IsActive = model.IsActive
                },
                cancellationToken);

            string successMessage = GetText("BackOffice.Organizations.Messages.Updated", "Organizasyon başarıyla güncellendi.");
            string redirectUrl = Url.Action(nameof(Index), "Organizations", new { culture = CurrentCulture() })
                ?? $"/{CurrentCulture()}/organizations/index";

            return Json(new
            {
                success = true,
                message = successMessage,
                redirectUrl
            });
        }
        catch (Exception exception)
        {
            string message = GetExceptionMessage(exception);

            return BadRequest(new
            {
                success = false,
                message
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new
            {
                success = false,
                message = GetText("Common.InvalidRequest", "Geçersiz istek.")
            });
        }

        try
        {
            UpdateOrganizationViewModel model = await BuildUpdateViewModelAsync(id, cancellationToken);

            return Json(new
            {
                success = true,
                data = new
                {
                    id = model.Id,
                    name = model.Name,
                    code = model.Code,
                    shortName = model.ShortName,
                    websiteUrl = model.WebsiteUrl,
                    description = model.Description,
                    contactName = model.ContactName,
                    contactTitle = model.ContactTitle,
                    contactEmail = model.ContactEmail,
                    contactPhone = model.ContactPhone,
                    contactNote = model.ContactNote,
                    brandColor = NormalizeBrandColor(model.BrandColor) ?? "#487FFF",
                    logoLightPath = model.ExistingLogoLightPath,
                    logoDarkPath = model.ExistingLogoDarkPath,
                    logoLightUrl = model.ExistingLogoLightUrl,
                    logoDarkUrl = model.ExistingLogoDarkUrl,
                    isActive = model.IsActive
                }
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
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new
            {
                success = false,
                message = GetText("Common.InvalidRequest", "Geçersiz istek.")
            });
        }

        try
        {
            await _mediator.Send(new DeleteOrganizationCommand { Id = id }, cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("BackOffice.Organizations.Messages.Deleted", "Organizasyon başarıyla silindi.")
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

    private async Task<UpdateOrganizationViewModel> BuildUpdateViewModelAsync(Guid id, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetByIdOrganizationQuery { Id = id }, cancellationToken);

        return new UpdateOrganizationViewModel
        {
            Id = response.Id,
            Name = response.Name,
            Code = response.Code,
            ShortName = response.ShortName,
            WebsiteUrl = response.WebsiteUrl,
            Description = response.Description,
            ContactName = response.ContactName,
            ContactTitle = response.ContactTitle,
            ContactEmail = response.ContactEmail,
            ContactPhone = response.ContactPhone,
            ContactNote = response.ContactNote,
            BrandColor = NormalizeBrandColor(response.BrandColor) ?? "#487FFF",
            ExistingLogoLightPath = response.LogoLightPath,
            ExistingLogoDarkPath = response.LogoDarkPath,
            ExistingLogoLightUrl = response.LogoLightUrl,
            ExistingLogoDarkUrl = response.LogoDarkUrl,
            IsActive = response.IsActive
        };
    }

    private void NormalizeOrganizationModelState(CreateOrganizationViewModel model)
    {
        ReplaceOrganizationFieldErrors(nameof(model.Name), GetNameValidationMessage(model.Name));
        ReplaceOrganizationFieldErrors(nameof(model.Code), GetCodeValidationMessage(model.Code));
        ReplaceOrganizationFieldErrors(nameof(model.WebsiteUrl), GetWebsiteUrlValidationMessage(model.WebsiteUrl));
        ReplaceOrganizationFieldErrors(nameof(model.ContactEmail), GetContactEmailValidationMessage(model.ContactEmail));
    }

    private void ReplaceOrganizationFieldErrors(string key, string? message)
    {
        if (!ModelState.TryGetValue(key, out Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateEntry? entry) || entry.Errors.Count == 0)
            return;

        entry.Errors.Clear();

        if (!string.IsNullOrWhiteSpace(message))
            entry.Errors.Add(message);
    }

    private string? GetNameValidationMessage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return GetText("BackOffice.Organizations.Validation.NameRequired", "Organizasyon adı zorunludur.");

        if (value.Trim().Length > 200)
            return GetText("BackOffice.Organizations.Validation.NameMaxLength", "Organizasyon adı en fazla 200 karakter olabilir.");

        return null;
    }

    private string? GetCodeValidationMessage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return GetText("BackOffice.Organizations.Validation.CodeRequired", "Organizasyon kodu zorunludur.");

        string normalizedValue = value.Trim();

        if (normalizedValue.Length > 80)
            return GetText("BackOffice.Organizations.Validation.CodeMaxLength", "Organizasyon kodu en fazla 80 karakter olabilir.");

        if (!OrganizationCodeRegex.IsMatch(normalizedValue))
            return GetText("BackOffice.Organizations.Validation.InvalidCode", "Organizasyon kodu yalnızca harf, rakam ve tire içermelidir.");

        return null;
    }

    private string? GetWebsiteUrlValidationMessage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return Uri.TryCreate(value.Trim(), UriKind.Absolute, out Uri? uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? null
            : GetText("BackOffice.Organizations.Validation.InvalidWebsiteUrl", "Geçerli bir web sitesi adresi giriniz.");
    }

    private string? GetContactEmailValidationMessage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        try
        {
            _ = new MailAddress(value.Trim());
            return null;
        }
        catch
        {
            return GetText("BackOffice.Organizations.Validation.InvalidContactEmail", "Geçerli bir e-posta adresi giriniz.");
        }
    }

    private object CreateValidationErrorResponse()
    {
        return new
        {
            success = false,
            message = GetText("Common.InvalidRequest", "Geçersiz istek."),
            errors = GetModelStateErrors()
        };
    }

    private Dictionary<string, string[]> GetModelStateErrors()
    {
        return ModelState
            .Where(item => item.Value is not null && item.Value.Errors.Count > 0)
            .ToDictionary(
                item => item.Key,
                item => item.Value!.Errors.Select(error => error.ErrorMessage).ToArray());
    }


    private string GetPostedBrandColor(string? modelValue)
    {
        string? postedValue = null;

        if (Request.HasFormContentType)
            postedValue = Request.Form["BrandColor"].FirstOrDefault();

        return NormalizeBrandColor(postedValue) ?? NormalizeBrandColor(modelValue) ?? "#487FFF";
    }

    private static string? NormalizeBrandColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        string color = value.Trim();

        if (color.Length != 7 || color[0] != '#' || !color.Skip(1).All(Uri.IsHexDigit))
            return null;

        return color.ToUpperInvariant();
    }

    private static string? FormatDate(DateTime? value)
    {
        if (value is null || value.Value == default)
            return null;

        return value.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
    }

    private string CurrentCulture()
    {
        return RouteData.Values["culture"]?.ToString() ?? "tr-TR";
    }

    private static string NormalizeCode(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private string GetText(string key, string fallback)
    {
        string value = _localizer.GetStringValue(key);
        return string.IsNullOrWhiteSpace(value) || string.Equals(value, key, StringComparison.OrdinalIgnoreCase)
            ? fallback
            : value;
    }

    private static string GetExceptionMessage(Exception exception)
    {
        return !string.IsNullOrWhiteSpace(exception.Message)
            ? exception.Message
            : "İşlem sırasında bir hata oluştu.";
    }
}
