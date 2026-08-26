using Core.Application.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressBoards.Commands.Create;
using Symplify.BackOffice.Application.Features.CongressBoards.Commands.Delete;
using Symplify.BackOffice.Application.Features.CongressBoards.Commands.Reorder;
using Symplify.BackOffice.Application.Features.CongressBoards.Commands.Update;
using Symplify.BackOffice.Application.Features.CongressBoards.Queries.GetById;
using Symplify.BackOffice.Application.Features.CongressBoards.Queries.GetForUpdate;
using Symplify.BackOffice.Application.Features.CongressBoards.Queries.GetList;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.WebUI.Models.CongressBoards;
using Symplify.BackOffice.WebUI.Models.Shared.DataTables;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture?}/congress-boards")]
public sealed class CongressBoardsController : Controller
{
    private const int DefaultPageIndex = 0;
    private const int DefaultPageSize = 50;

    private readonly IMediator _mediator;
    private readonly IApplicationLanguageProvider _applicationLanguageProvider;
    private readonly IBackOfficeViewLocalizer _localizer;

    public CongressBoardsController(
        IMediator mediator,
        IApplicationLanguageProvider applicationLanguageProvider,
        IBackOfficeViewLocalizer localizer)
    {
        _mediator = mediator;
        _applicationLanguageProvider = applicationLanguageProvider;
        _localizer = localizer;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken, int page = DefaultPageIndex, int pageSize = DefaultPageSize)
    {
        var response = await _mediator.Send(new GetListCongressBoardQuery
        {
            PageRequest = new PageRequest
            {
                Page = page,
                PageSize = pageSize
            }
        }, cancellationToken);

        return View(response);
    }

    [HttpGet("details/{id}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest();

        var response = await _mediator.Send(new GetByIdCongressBoardQuery { Id = id }, cancellationToken);

        return View(response);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return View(new CreateCongressBoardCommand());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCongressBoardCommand command, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(command);

        await _mediator.Send(command, cancellationToken);

        TempData["SuccessMessage"] = "Congress Boards kaydı oluşturuldu.";
        return RedirectToIndex();
    }

    [HttpGet("edit/{id}")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest();

        var response = await _mediator.Send(new GetByIdCongressBoardQuery { Id = id }, cancellationToken);

        UpdateCongressBoardCommand command = new()
        {
            Id = response.Id,
            CongressId = response.CongressId,
            Order = response.Order,
            IsActive = response.IsActive
        };

        return View(command);
    }

    [HttpPost("edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateCongressBoardCommand command, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(command);

        await _mediator.Send(command, cancellationToken);

        TempData["SuccessMessage"] = "Congress Boards kaydı güncellendi.";
        return RedirectToIndex();
    }

    [HttpPost("delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(DeleteCongressBoardCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);

        TempData["SuccessMessage"] = "Congress Boards kaydı silindi.";
        return RedirectToIndex();
    }

    [HttpGet("manage-list")]
    public async Task<IActionResult> GetManageList(Guid congressId, CancellationToken cancellationToken)
    {
        if (congressId == Guid.Empty)
            return Json(new { success = true, items = Array.Empty<object>() });

        string culture = await ResolveCurrentCultureAsync(cancellationToken);

        var response = await _mediator.Send(new GetListCongressBoardQuery
        {
            CongressId = congressId,
            Culture = culture,
            BypassCache = true,
            PageRequest = new PageRequest { Page = 0, PageSize = 500 }
        }, cancellationToken);

        return Json(new
        {
            success = true,
            items = response.Items
                .OrderBy(item => item.Order <= 0 ? int.MaxValue : item.Order)
                .ThenBy(item => item.Name)
                .Select(item => new
                {
                    id = item.Id,
                    congressId = item.CongressId,
                    order = item.Order,
                    name = item.Name,
                    description = item.Description,
                    isActive = item.IsActive,
                    isFallback = item.IsFallback
                })
        });
    }

    [HttpGet("create-modal")]
    public async Task<IActionResult> CreateManageModal(Guid congressId, CancellationToken cancellationToken)
    {
        CreateCongressBoardManageViewModel model = new()
        {
            CongressId = congressId,
            IsActive = true,
            Translations = await BuildTranslationViewModelsAsync(cancellationToken)
        };

        return PartialView("~/Views/CongressBoards/_CreateBoardModal.cshtml", model);
    }

    [HttpPost("create-manage")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateManage([FromForm] CreateCongressBoardManageViewModel model, CancellationToken cancellationToken)
    {
        ValidateBoardModel(model.CongressId, model.Translations);

        if (!ModelState.IsValid)
            return BadRequest(CreateValidationErrorResponse());

        try
        {
            CreatedCongressBoardResponse response = await _mediator.Send(new CreateCongressBoardCommand
            {
                CongressId = model.CongressId,
                IsActive = model.IsActive,
                Order = 0,
                Translations = BuildTranslationInputs(model.Translations)
            }, cancellationToken);

            return Json(new
            {
                success = true,
                id = response.Id,
                message = GetText("BackOffice.CongressBoards.Messages.Created", "Kurul başarıyla oluşturuldu.")
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new { success = false, message = GetExceptionMessage(exception) });
        }
    }

    [HttpGet("edit-modal")]
    public async Task<IActionResult> EditManageModal(Guid id, Guid congressId, CancellationToken cancellationToken)
    {
        GetCongressBoardForUpdateResponse response = await _mediator.Send(new GetCongressBoardForUpdateQuery { Id = id }, cancellationToken);

        if (response.CongressId != congressId)
            return BadRequest(new { success = false, message = GetText("Common.InvalidRequest", "Geçersiz istek.") });

        UpdateCongressBoardManageViewModel model = new()
        {
            Id = response.Id,
            CongressId = response.CongressId,
            Order = response.Order,
            IsActive = response.IsActive,
            Translations = response.Translations.Select(translation => new CongressBoardTranslationViewModel
            {
                LanguageId = translation.LanguageId,
                Culture = translation.Culture,
                LanguageName = translation.LanguageName,
                IsDefault = translation.IsDefault,
                Exists = translation.Exists,
                Name = GetField(translation.Fields, "Name"),
                Description = GetField(translation.Fields, "Description")
            }).ToList()
        };

        return PartialView("~/Views/CongressBoards/_UpdateBoardModal.cshtml", model);
    }

    [HttpPost("update-manage")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateManage([FromForm] UpdateCongressBoardManageViewModel model, CancellationToken cancellationToken)
    {
        if (model.Id == Guid.Empty)
            ModelState.AddModelError(nameof(model.Id), GetText("Common.InvalidRequest", "Geçersiz istek."));

        ValidateBoardModel(model.CongressId, model.Translations);

        if (!ModelState.IsValid)
            return BadRequest(CreateValidationErrorResponse());

        try
        {
            await _mediator.Send(new UpdateCongressBoardCommand
            {
                Id = model.Id,
                CongressId = model.CongressId,
                Order = model.Order,
                IsActive = model.IsActive,
                Translations = BuildTranslationInputs(model.Translations)
            }, cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("BackOffice.CongressBoards.Messages.Updated", "Kurul başarıyla güncellendi.")
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new { success = false, message = GetExceptionMessage(exception) });
        }
    }


    [HttpPost("reorder-manage")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReorderManage(
        [FromBody] DataTableReorderRequest request,
        [FromQuery] Guid congressId,
        CancellationToken cancellationToken)
    {
        if (congressId == Guid.Empty || request.Items.Count == 0)
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
                new ReorderCongressBoardCommand
                {
                    CongressId = congressId,
                    Items = request.Items
                        .Where(item => item.Id != Guid.Empty)
                        .Select(item => new ReorderCongressBoardItemDto
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
                message = GetText("Common.Updated", "Kayıt güncellendi.")
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

    [HttpPost("delete-manage")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteManage([FromForm] Guid id, [FromForm] Guid congressId, CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new DeleteCongressBoardCommand { Id = id }, cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("BackOffice.CongressBoards.Messages.Deleted", "Kurul başarıyla silindi.")
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new { success = false, message = GetExceptionMessage(exception) });
        }
    }

    private RedirectToActionResult RedirectToIndex()
    {
        string? culture = RouteData.Values["culture"]?.ToString();

        if (string.IsNullOrWhiteSpace(culture))
            return RedirectToAction(nameof(Index));

        return RedirectToAction(nameof(Index), new { culture });
    }

    private async Task<List<CongressBoardTranslationViewModel>> BuildTranslationViewModelsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ApplicationLanguageDto> languages = await _applicationLanguageProvider.GetActiveLanguagesAsync(cancellationToken);

        return languages
            .OrderByDescending(language => language.IsDefault)
            .ThenBy(language => language.Order)
            .ThenBy(language => language.Name)
            .Select(language => new CongressBoardTranslationViewModel
            {
                LanguageId = language.Id,
                Culture = language.Culture,
                LanguageName = language.Name,
                IsDefault = language.IsDefault,
                Exists = false
            })
            .ToList();
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

    private void ValidateBoardModel(Guid congressId, IEnumerable<CongressBoardTranslationViewModel> translations)
    {
        if (congressId == Guid.Empty)
            ModelState.AddModelError("CongressId", GetText("BackOffice.CongressBoards.Validation.CongressRequired", "Kongre bilgisi zorunludur."));

        CongressBoardTranslationViewModel? defaultTranslation = translations.FirstOrDefault(translation => translation.IsDefault);

        if (defaultTranslation is null || string.IsNullOrWhiteSpace(defaultTranslation.Name))
            ModelState.AddModelError("Translations", GetText("BackOffice.CongressBoards.Validation.DefaultNameRequired", "Varsayılan dilde kurul adı zorunludur."));
    }

    private ICollection<TranslationInputDto> BuildTranslationInputs(IEnumerable<CongressBoardTranslationViewModel> translations)
    {
        return translations
            .GroupBy(translation => translation.LanguageId)
            .Select(group => group.First())
            .Where(translation => translation.IsDefault || !string.IsNullOrWhiteSpace(translation.Name) || !string.IsNullOrWhiteSpace(translation.Description))
            .Select(translation => new TranslationInputDto
            {
                LanguageId = translation.LanguageId,
                Fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Name"] = NormalizeText(translation.Name),
                    ["Description"] = NormalizeText(translation.Description)
                }
            })
            .ToList();
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

    private string GetText(string key, string fallback)
    {
        string value = _localizer.GetStringValue(key);

        return string.IsNullOrWhiteSpace(value) ? key : value;
    }

    private string GetExceptionMessage(Exception exception)
    {
        return !string.IsNullOrWhiteSpace(exception.Message)
            ? GetText(exception.Message, exception.Message)
            : GetText("Common.GenericError", string.Empty);
    }

    private static string GetTwoLetterIsoCode(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
            return string.Empty;

        string normalizedCulture = culture.Trim();
        int separatorIndex = normalizedCulture.IndexOf('-');

        return separatorIndex > 0 ? normalizedCulture[..separatorIndex] : normalizedCulture;
    }

    private static string? NormalizeText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? GetField(IDictionary<string, string?> fields, string key)
        => fields.TryGetValue(key, out string? value) ? value : null;
}
