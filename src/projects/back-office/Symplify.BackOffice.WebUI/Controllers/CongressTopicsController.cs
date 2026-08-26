using Core.Application.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.Application.Features.CongressTopics.Commands.SyncSelections;
using Symplify.BackOffice.Application.Features.CongressTopicCategories.Commands.Save;
using Symplify.BackOffice.Application.Features.CongressTopicCategories.Queries.GetList;
using Symplify.BackOffice.Application.Features.CongressTopics.Constants;
using Symplify.BackOffice.Application.Features.CongressTopics.Queries.GetList;
using Symplify.BackOffice.Application.Features.CongressTopics.Queries.GetSelectionList;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.WebUI.Models.CongressTopics;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture=tr-TR}/[controller]/[action]")]
public sealed class CongressTopicsController : Controller
{
    private const string SafeFallbackCulture = "tr-TR";

    private readonly IMediator _mediator;
    private readonly IApplicationLanguageProvider _applicationLanguageProvider;
    private readonly IBackOfficeViewLocalizer _localizer;

    public CongressTopicsController(
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
            new GetListCongressTopicQuery
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

        IReadOnlyList<GetCongressTopicCategoryListItemDto> categories = await _mediator.Send(
            new GetCongressTopicCategoryListQuery
            {
                CongressId = congressId,
                Culture = culture
            },
            cancellationToken);

        Dictionary<Guid, string> categoryNames = categories.ToDictionary(item => item.Id, item => item.Name);

        return Json(new
        {
            success = true,
            items = response.Items.Select(item => new
            {
                id = item.Id,
                topicId = item.TopicId,
                categoryId = item.CategoryId,
                categoryName = item.CategoryId.HasValue && categoryNames.TryGetValue(item.CategoryId.Value, out string? categoryName)
                    ? categoryName
                    : null,
                code = item.Code,
                name = item.Name,
                description = item.Description,
                order = item.Order,
                isActive = item.IsActive,
                topicIsActive = item.TopicIsActive,
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

        IReadOnlyList<GetCongressTopicSelectionListItemDto> items = await _mediator.Send(
            new GetCongressTopicSelectionListQuery
            {
                CongressId = congressId,
                Culture = culture
            },
            cancellationToken);

        IReadOnlyList<GetCongressTopicCategoryListItemDto> categories = await _mediator.Send(
            new GetCongressTopicCategoryListQuery
            {
                CongressId = congressId,
                Culture = culture
            },
            cancellationToken);

        return Json(new
        {
            success = true,
            categories = categories.Select(item => new
            {
                id = item.Id,
                name = item.Name,
                order = item.Order,
                isActive = item.IsActive
            }),
            items = items.Select(item => new
            {
                topicId = item.TopicId,
                congressTopicId = item.CongressTopicId,
                categoryId = item.CategoryId,
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
        [FromForm] SaveCongressTopicSelectionsViewModel model,
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
            SyncedCongressTopicSelectionsResponse response = await _mediator.Send(
                new SyncCongressTopicSelectionsCommand
                {
                    CongressId = model.CongressId,
                    SelectedTopicIds = model.SelectedTopicIds,
                    Assignments = model.SelectedTopicIds
                        .Where(id => id != Guid.Empty)
                        .Select((topicId, index) => new CongressTopicSelectionAssignmentDto
                        {
                            TopicId = topicId,
                            CategoryId = index < model.SelectedCategoryIds.Count
                                ? model.SelectedCategoryIds[index]
                                : null
                        })
                        .ToList()
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                selectedCount = response.SelectedCount,
                message = GetText(CongressTopicsMessages.Saved, "Kongre konu seçimleri güncellendi.")
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
    public async Task<IActionResult> GetCategories(Guid congressId, CancellationToken cancellationToken)
    {
        if (congressId == Guid.Empty)
            return BadRequest(new { success = false, message = GetText("Common.InvalidRequest", "Geçersiz istek.") });

        string culture = await ResolveCurrentCultureAsync(cancellationToken);
        IReadOnlyList<ApplicationLanguageDto> languages = await _applicationLanguageProvider.GetActiveLanguagesAsync(cancellationToken);
        IReadOnlyList<GetCongressTopicCategoryListItemDto> categories = await _mediator.Send(
            new GetCongressTopicCategoryListQuery { CongressId = congressId, Culture = culture },
            cancellationToken);

        return Json(new
        {
            success = true,
            languages = languages
                .OrderByDescending(item => item.IsDefault)
                .ThenBy(item => item.Order)
                .ThenBy(item => item.Name)
                .Select(item => new
                {
                    id = item.Id,
                    culture = item.Culture,
                    name = item.Name,
                    isDefault = item.IsDefault
                }),
            categories = categories.Select(item => new
            {
                id = item.Id,
                name = item.Name,
                order = item.Order,
                isActive = item.IsActive,
                translations = item.Translations.Select(translation => new
                {
                    languageId = translation.LanguageId,
                    name = translation.Name
                })
            })
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCategories(
        [FromBody] SaveCongressTopicCategoriesViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.CongressId == Guid.Empty)
            return BadRequest(new { success = false, message = GetText("Common.InvalidRequest", "Geçersiz istek.") });

        try
        {
            SavedCongressTopicCategoriesResponse response = await _mediator.Send(
                new SaveCongressTopicCategoriesCommand
                {
                    CongressId = model.CongressId,
                    Categories = model.Categories.Select(item => new SaveCongressTopicCategoryItemDto
                    {
                        Id = item.Id,
                        Order = item.Order,
                        IsActive = item.IsActive,
                        Translations = item.Translations.Select(translation => new SaveCongressTopicCategoryTranslationDto
                        {
                            LanguageId = translation.LanguageId,
                            Name = translation.Name
                        }).ToList()
                    }).ToList()
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                categoryCount = response.CategoryCount,
                message = GetText("BackOffice.CongressTopics.Messages.CategoriesSaved", "Konu kategorileri kaydedildi.")
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new { success = false, message = GetExceptionMessage(exception) });
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
