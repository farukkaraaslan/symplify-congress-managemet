using Core.Application.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Commands.SyncSelections;
using Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Constants;
using Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Queries.GetList;
using Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Queries.GetSelectionList;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.WebUI.Models.CongressSubmissionTypes;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture=tr-TR}/[controller]/[action]")]
public sealed class CongressSubmissionTypesController : Controller
{
    private const string SafeFallbackCulture = "tr-TR";

    private readonly IMediator _mediator;
    private readonly IApplicationLanguageProvider _applicationLanguageProvider;
    private readonly IBackOfficeViewLocalizer _localizer;

    public CongressSubmissionTypesController(
        IMediator mediator,
        IApplicationLanguageProvider applicationLanguageProvider,
        IBackOfficeViewLocalizer localizer)
    {
        _mediator = mediator;
        _applicationLanguageProvider = applicationLanguageProvider;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> GetSelected(Guid congressId, CancellationToken cancellationToken)
    {
        if (congressId == Guid.Empty)
        {
            return Json(new
            {
                success = true,
                items = Array.Empty<object>()
            });
        }

        string culture = await ResolveCurrentCultureAsync(cancellationToken);

        var response = await _mediator.Send(
            new GetListCongressSubmissionTypeQuery
            {
                CongressId = congressId,
                Culture = culture,
                IsActive = true,
                SortColumn = "order",
                SortDirection = "asc",
                PageRequest = new PageRequest
                {
                    Page = 0,
                    PageSize = 500
                }
            },
            cancellationToken);

        return Json(new
        {
            success = true,
            items = response.Items.Select(item => new
            {
                id = item.Id,
                submissionTypeId = item.SubmissionTypeId,
                code = item.Code,
                name = item.Name,
                description = item.Description,
                order = item.Order,
                isActive = item.IsActive,
                submissionTypeIsActive = item.SubmissionTypeIsActive,
                isFallback = item.IsFallback
            })
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetSelectionOptions(Guid congressId, CancellationToken cancellationToken)
    {
        if (congressId == Guid.Empty)
        {
            return BadRequest(new
            {
                success = false,
                message = GetText("Common.InvalidRequest", "Geçersiz istek.")
            });
        }

        string culture = await ResolveCurrentCultureAsync(cancellationToken);

        IReadOnlyList<GetCongressSubmissionTypeSelectionListItemDto> items = await _mediator.Send(
            new GetCongressSubmissionTypeSelectionListQuery
            {
                CongressId = congressId,
                Culture = culture
            },
            cancellationToken);

        return Json(new
        {
            success = true,
            items = items.Select(item => new
            {
                submissionTypeId = item.SubmissionTypeId,
                congressSubmissionTypeId = item.CongressSubmissionTypeId,
                code = item.Code,
                name = item.Name,
                description = item.Description,
                order = item.Order,
                isActive = item.IsActive,
                isSelected = item.IsSelected,
                isFallback = item.IsFallback
            })
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSelections(
        [FromForm] SaveCongressSubmissionTypeSelectionsViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.CongressId == Guid.Empty)
        {
            return BadRequest(new
            {
                success = false,
                message = GetText("Common.InvalidRequest", "Geçersiz istek.")
            });
        }

        try
        {
            SyncedCongressSubmissionTypeSelectionsResponse response = await _mediator.Send(
                new SyncCongressSubmissionTypeSelectionsCommand
                {
                    CongressId = model.CongressId,
                    SelectedSubmissionTypeIds = model.SelectedSubmissionTypeIds
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                selectedCount = response.SelectedCount,
                message = GetText(CongressSubmissionTypesMessages.Saved, "Kongre bildiri türü seçimleri güncellendi.")
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

    private async Task<string> ResolveCurrentCultureAsync(CancellationToken cancellationToken)
    {
        string? headerCulture = Request.Headers["X-Culture"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(headerCulture))
            return await NormalizeCultureFromApplicationLanguagesAsync(headerCulture, cancellationToken);

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

        if (activeLanguages.Count == 0)
            return SafeFallbackCulture;

        if (string.IsNullOrWhiteSpace(culture))
            return activeLanguages.FirstOrDefault(language => language.IsDefault)?.Culture ?? activeLanguages[0].Culture;

        ApplicationLanguageDto? language = activeLanguages.FirstOrDefault(item =>
            string.Equals(item.Culture, culture, StringComparison.OrdinalIgnoreCase));

        return language?.Culture
            ?? activeLanguages.FirstOrDefault(item => item.IsDefault)?.Culture
            ?? activeLanguages[0].Culture;
    }

    private string GetExceptionMessage(Exception exception)
    {
        return !string.IsNullOrWhiteSpace(exception.Message)
            ? GetText(exception.Message, exception.Message)
            : GetText("Common.GenericError", string.Empty);
    }

    private string GetText(string key, string fallback)
    {
        string value = _localizer.GetStringValue(key);

        return string.IsNullOrWhiteSpace(value) ? key : value;
    }
}
