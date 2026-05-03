using Core.Application.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Queries.GetList;
using Symplify.BackOffice.Application.Features.WorkflowTemplateTransitions.Commands.Create;
using Symplify.BackOffice.Application.Features.WorkflowTemplateTransitions.Commands.Delete;
using Symplify.BackOffice.Application.Features.WorkflowTemplateTransitions.Commands.Reorder;
using Symplify.BackOffice.Application.Features.WorkflowTemplateTransitions.Commands.Update;
using Symplify.BackOffice.Application.Features.WorkflowTemplateTransitions.Queries.GetList;
using Symplify.BackOffice.Application.Features.WorkflowTemplates.Queries.GetById;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.WebUI.Models.Shared.DataTables;
using Symplify.BackOffice.WebUI.Models.WorkflowTemplateTransitions;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture=tr-TR}/[controller]/[action]")]
public sealed class WorkflowTemplateTransitionsController : Controller
{
    private const string SafeFallbackCulture = "tr-TR";

    private readonly IMediator _mediator;
    private readonly IApplicationLanguageProvider _applicationLanguageProvider;
    private readonly IBackOfficeViewLocalizer _localizer;

    public WorkflowTemplateTransitionsController(
        IMediator mediator,
        IApplicationLanguageProvider applicationLanguageProvider,
        IBackOfficeViewLocalizer localizer)
    {
        _mediator = mediator;
        _applicationLanguageProvider = applicationLanguageProvider;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid workflowTemplateId, CancellationToken cancellationToken)
    {
        if (workflowTemplateId == Guid.Empty)
        {
            return BadRequest(GetText("Common.InvalidRequest", "Geçersiz istek."));
        }

        WorkflowTemplateTransitionsIndexViewModel model = await BuildIndexViewModelAsync(workflowTemplateId, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GetList(
        [FromForm] DataTableRequest request,
        Guid workflowTemplateId,
        CancellationToken cancellationToken)
    {
        if (workflowTemplateId == Guid.Empty)
        {
            return BadRequest(new { success = false, message = GetText("Common.InvalidRequest", "Geçersiz istek.") });
        }

        string culture = await ResolveCurrentCultureAsync(cancellationToken);

        IList<GetListWorkflowTemplateTransitionListItemDto> response = await _mediator.Send(
            new GetListWorkflowTemplateTransitionQuery
            {
                WorkflowTemplateId = workflowTemplateId,
                Culture = culture
            },
            cancellationToken);

        int start = Math.Max(request.Start, 0);
        int pageSize = request.Length <= 0 ? response.Count : request.Length;

        List<object> pageItems = response
            .Skip(start)
            .Take(pageSize)
            .Select((item, index) => new
            {
                rowNumber = start + index + 1,
                id = item.Id,
                workflowTemplateId = item.WorkflowTemplateId,
                transactionStatusTransitionId = item.TransactionStatusTransitionId,
                fromStatusName = item.FromStatusName,
                toStatusName = item.ToStatusName,
                transitionName = item.TransitionName,
                transitionDescription = item.TransitionDescription,
                order = item.Order,
                isActive = item.IsActive
            })
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromForm] CreateWorkflowTemplateTransitionViewModel model,
        CancellationToken cancellationToken)
    {
        ValidateCreateModel(model);

        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                message = GetText("Common.InvalidRequest", "Geçersiz istek."),
                errors = GetModelStateErrors()
            });
        }

        try
        {
            await _mediator.Send(
                new CreateWorkflowTemplateTransitionCommand
                {
                    WorkflowTemplateId = model.WorkflowTemplateId,
                    TransactionStatusTransitionId = model.TransactionStatusTransitionId,
                    IsActive = model.IsActive
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("BackOffice.WorkflowTemplateTransitions.Messages.Created", "Workflow şablon geçişi oluşturuldu.")
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new { success = false, message = GetExceptionMessage(exception) });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        [FromForm] UpdateWorkflowTemplateTransitionViewModel model,
        CancellationToken cancellationToken)
    {
        ValidateUpdateModel(model);

        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                message = GetText("Common.InvalidRequest", "Geçersiz istek."),
                errors = GetModelStateErrors()
            });
        }

        try
        {
            await _mediator.Send(
                new UpdateWorkflowTemplateTransitionCommand
                {
                    Id = model.Id,
                    TransactionStatusTransitionId = model.TransactionStatusTransitionId,
                    IsActive = model.IsActive
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("BackOffice.WorkflowTemplateTransitions.Messages.Updated", "Workflow şablon geçişi güncellendi.")
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new { success = false, message = GetExceptionMessage(exception) });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new { success = false, message = GetText("Common.InvalidRequest", "Geçersiz istek.") });
        }

        try
        {
            await _mediator.Send(new DeleteWorkflowTemplateTransitionCommand { Id = id }, cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("BackOffice.WorkflowTemplateTransitions.Messages.Deleted", "Workflow şablon geçişi silindi.")
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new { success = false, message = GetExceptionMessage(exception) });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder(
        [FromBody] DataTableReorderRequest request,
        Guid workflowTemplateId,
        CancellationToken cancellationToken)
    {
        if (workflowTemplateId == Guid.Empty || request.Items.Count == 0)
        {
            return BadRequest(new { success = false, message = GetText("Common.InvalidRequest", "Geçersiz istek.") });
        }

        try
        {
            await _mediator.Send(
                new ReorderWorkflowTemplateTransitionCommand
                {
                    WorkflowTemplateId = workflowTemplateId,
                    Items = request.Items
                        .Where(item => item.Id != Guid.Empty)
                        .Select(item => new ReorderWorkflowTemplateTransitionItemDto
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
                message = GetText("BackOffice.WorkflowTemplateTransitions.Messages.Reordered", "Workflow şablon geçiş sıralaması güncellendi.")
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new { success = false, message = GetExceptionMessage(exception) });
        }
    }

    private async Task<WorkflowTemplateTransitionsIndexViewModel> BuildIndexViewModelAsync(Guid workflowTemplateId, CancellationToken cancellationToken)
    {
        string culture = await ResolveCurrentCultureAsync(cancellationToken);

        GetByIdWorkflowTemplateResponse template = await _mediator.Send(
            new GetByIdWorkflowTemplateQuery
            {
                Id = workflowTemplateId,
                Culture = culture
            },
            cancellationToken);

        List<TransactionStatusTransitionSelectItemViewModel> availableTransitions = await GetTransitionSelectItemsAsync(culture, cancellationToken);

        return new WorkflowTemplateTransitionsIndexViewModel
        {
            WorkflowTemplateId = workflowTemplateId,
            WorkflowTemplateCode = template.Code,
            WorkflowTemplateName = template.Name,
            AvailableTransitions = availableTransitions,
            CreateTransition = new CreateWorkflowTemplateTransitionViewModel
            {
                WorkflowTemplateId = workflowTemplateId,
                IsActive = true
            },
            UpdateTransition = new UpdateWorkflowTemplateTransitionViewModel
            {
                WorkflowTemplateId = workflowTemplateId,
                IsActive = true
            }
        };
    }

    private async Task<List<TransactionStatusTransitionSelectItemViewModel>> GetTransitionSelectItemsAsync(
        string culture,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(
            new GetListTransactionStatusTransitionQuery
            {
                Culture = culture,
                IsActive = true,
                SortColumn = "fromStatus",
                SortDirection = "asc",
                PageRequest = new PageRequest
                {
                    Page = 0,
                    PageSize = 2000
                }
            },
            cancellationToken);

        return response.Items.Select(item => new TransactionStatusTransitionSelectItemViewModel
        {
            Id = item.Id,
            FromStatusName = item.FromStatusName,
            FromStatusCode = item.FromStatusCode,
            ToStatusName = item.ToStatusName,
            ToStatusCode = item.ToStatusCode,
            Name = item.Name
        }).ToList();
    }

    private void ValidateCreateModel(CreateWorkflowTemplateTransitionViewModel model)
    {
        if (model.WorkflowTemplateId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(model.WorkflowTemplateId), GetText("Common.InvalidRequest", "Geçersiz istek."));
        }

        if (model.TransactionStatusTransitionId <= 0)
        {
            ModelState.AddModelError(nameof(model.TransactionStatusTransitionId), GetText("BackOffice.WorkflowTemplateTransitions.Validation.TransitionRequired", "Geçiş seçimi zorunludur."));
        }
    }

    private void ValidateUpdateModel(UpdateWorkflowTemplateTransitionViewModel model)
    {
        if (model.Id == Guid.Empty)
        {
            ModelState.AddModelError(nameof(model.Id), GetText("Common.InvalidRequest", "Geçersiz istek."));
        }

        if (model.WorkflowTemplateId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(model.WorkflowTemplateId), GetText("Common.InvalidRequest", "Geçersiz istek."));
        }

        if (model.TransactionStatusTransitionId <= 0)
        {
            ModelState.AddModelError(nameof(model.TransactionStatusTransitionId), GetText("BackOffice.WorkflowTemplateTransitions.Validation.TransitionRequired", "Geçiş seçimi zorunludur."));
        }
    }

    private Dictionary<string, string[]> GetModelStateErrors()
    {
        return ModelState
            .Where(item => item.Value is not null && item.Value.Errors.Count > 0)
            .ToDictionary(
                item => item.Key,
                item => item.Value!.Errors.Select(error => error.ErrorMessage).ToArray());
    }

    private async Task<string> ResolveCurrentCultureAsync(CancellationToken cancellationToken)
    {
        string? formCulture = Request.HasFormContentType ? Request.Form["culture"].FirstOrDefault() : null;
        if (!string.IsNullOrWhiteSpace(formCulture)) return await NormalizeCultureFromApplicationLanguagesAsync(formCulture, cancellationToken);

        string? queryCulture = Request.Query["culture"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(queryCulture)) return await NormalizeCultureFromApplicationLanguagesAsync(queryCulture, cancellationToken);

        string? headerCulture = Request.Headers["X-Culture"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(headerCulture)) return await NormalizeCultureFromApplicationLanguagesAsync(headerCulture, cancellationToken);

        string? routeCulture = RouteData.Values["culture"]?.ToString();
        if (!string.IsNullOrWhiteSpace(routeCulture)) return await NormalizeCultureFromApplicationLanguagesAsync(routeCulture, cancellationToken);

        string? pathCulture = HttpContext.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
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

            if (matchedLanguage is not null) return matchedLanguage.Culture;
        }

        ApplicationLanguageDto defaultLanguage = await _applicationLanguageProvider.GetDefaultLanguageAsync(cancellationToken);

        return !string.IsNullOrWhiteSpace(defaultLanguage.Culture)
            ? defaultLanguage.Culture
            : activeLanguages.OrderByDescending(language => language.IsDefault).ThenBy(language => language.Name).FirstOrDefault()?.Culture ?? SafeFallbackCulture;
    }

    private static string GetTwoLetterIsoCode(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture)) return string.Empty;

        string normalizedCulture = culture.Trim();
        int separatorIndex = normalizedCulture.IndexOf('-');

        return separatorIndex > 0 ? normalizedCulture[..separatorIndex] : normalizedCulture;
    }

    private string GetText(string key, string fallback)
    {
        string value = _localizer.GetStringValue(key);
        return string.IsNullOrWhiteSpace(value) || string.Equals(value, key, StringComparison.OrdinalIgnoreCase) ? fallback : value;
    }

    private static string GetExceptionMessage(Exception exception)
    {
        return !string.IsNullOrWhiteSpace(exception.Message) ? exception.Message : "İşlem sırasında bir hata oluştu.";
    }
}
