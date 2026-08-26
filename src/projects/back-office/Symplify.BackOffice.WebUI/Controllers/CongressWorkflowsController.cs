using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressWorkflows.Commands.ApplyTemplate;
using Symplify.BackOffice.Application.Features.CongressWorkflows.Queries.GetByCongressId;
using Symplify.BackOffice.Application.Features.CongressWorkflows.Queries.GetTemplateOptions;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.WebUI.Models.CongressWorkflows;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture=tr-TR}/[controller]/[action]")]
public sealed class CongressWorkflowsController : Controller
{
    private const string SafeFallbackCulture = "tr-TR";

    private readonly IMediator _mediator;
    private readonly IApplicationLanguageProvider _applicationLanguageProvider;
    private readonly IBackOfficeViewLocalizer _localizer;

    public CongressWorkflowsController(
        IMediator mediator,
        IApplicationLanguageProvider applicationLanguageProvider,
        IBackOfficeViewLocalizer localizer)
    {
        _mediator = mediator;
        _applicationLanguageProvider = applicationLanguageProvider;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Get(Guid congressId, CancellationToken cancellationToken)
    {
        if (congressId == Guid.Empty)
            return BadRequest(new { success = false, message = GetText("Common.InvalidRequest", "Geçersiz istek.") });

        string culture = await ResolveCurrentCultureAsync(cancellationToken);

        try
        {
            GetCongressWorkflowByCongressIdResponse workflow = await _mediator.Send(
                new GetCongressWorkflowByCongressIdQuery
                {
                    CongressId = congressId,
                    Culture = culture
                },
                cancellationToken);

            GetCongressWorkflowTemplateOptionsResponse templateOptions = await _mediator.Send(
                new GetCongressWorkflowTemplateOptionsQuery
                {
                    Culture = culture,
                    OnlyActive = true
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                workflow = new
                {
                    congressId = workflow.CongressId,
                    sourceWorkflowTemplateId = workflow.SourceWorkflowTemplateId,
                    initialTransactionStatusId = workflow.InitialTransactionStatusId,
                    initialTransactionStatusName = workflow.InitialTransactionStatusName,
                    isActive = workflow.IsActive,
                    transitions = workflow.Transitions.Select(transition => new
                    {
                        id = transition.Id,
                        transactionStatusTransitionId = transition.TransactionStatusTransitionId,
                        fromStatusName = transition.FromStatusName,
                        toStatusName = transition.ToStatusName,
                        transitionName = transition.TransitionName,
                        transitionDescription = transition.TransitionDescription,
                        order = transition.Order,
                        isActive = transition.IsActive
                    }).ToList()
                },
                templates = templateOptions.Items.Select(template => new
                {
                    id = template.Id,
                    code = template.Code,
                    name = template.Name,
                    description = template.Description,
                    initialTransactionStatusId = template.InitialTransactionStatusId,
                    initialTransactionStatusName = template.InitialTransactionStatusName,
                    isDefault = template.IsDefault,
                    isActive = template.IsActive,
                    isFallback = template.IsFallback
                }).ToList()
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new { success = false, message = GetExceptionMessage(exception) });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyTemplate(
        [FromForm] ApplyCongressWorkflowTemplateViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.CongressId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(model.CongressId),
                GetText("BackOffice.CongressWorkflows.Validation.CongressNotFound", "Kongre bulunamadı."));
        }

        if (model.WorkflowTemplateId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(model.WorkflowTemplateId),
                GetText("BackOffice.CongressWorkflows.Validation.TemplateRequired", "Workflow şablonu seçimi zorunludur."));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                message = GetText("Common.InvalidRequest", "Form alanlarını kontrol edin."),
                errors = GetModelStateErrors()
            });
        }

        try
        {
            await _mediator.Send(
                new ApplyWorkflowTemplateToCongressCommand
                {
                    CongressId = model.CongressId,
                    WorkflowTemplateId = model.WorkflowTemplateId,
                    ReplaceExistingTransitions = model.ReplaceExistingTransitions
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText(
                    "BackOffice.CongressWorkflows.Messages.TemplateApplied",
                    "Workflow şablonu kongreye başarıyla uygulandı.")
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new { success = false, message = GetExceptionMessage(exception) });
        }
    }

    private Dictionary<string, string[]> GetModelStateErrors()
    {
        return ModelState
            .Where(item => item.Value?.Errors.Count > 0)
            .ToDictionary(
                item => item.Key,
                item => item.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? GetText("Common.InvalidRequest", "Geçersiz değer.")
                        : error.ErrorMessage)
                    .ToArray());
    }

    private async Task<string> ResolveCurrentCultureAsync(CancellationToken cancellationToken)
    {
        string? formCulture = Request.HasFormContentType ? Request.Form["culture"].FirstOrDefault() : null;

        if (!string.IsNullOrWhiteSpace(formCulture))
            return await NormalizeCultureFromApplicationLanguagesAsync(formCulture, cancellationToken);

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

        if (!string.IsNullOrWhiteSpace(culture))
        {
            ApplicationLanguageDto? exactMatch = activeLanguages.FirstOrDefault(language =>
                string.Equals(language.Culture, culture, StringComparison.OrdinalIgnoreCase));

            if (exactMatch is not null)
                return exactMatch.Culture;

            ApplicationLanguageDto? shortMatch = activeLanguages.FirstOrDefault(language =>
                string.Equals(GetShortCulture(language.Culture), culture, StringComparison.OrdinalIgnoreCase));

            if (shortMatch is not null)
                return shortMatch.Culture;
        }

        return activeLanguages.FirstOrDefault(language => language.IsDefault)?.Culture
            ?? SafeFallbackCulture;
    }

    private static string GetShortCulture(string culture)
    {
        int separatorIndex = culture.IndexOf('-', StringComparison.Ordinal);
        return separatorIndex > 0 ? culture[..separatorIndex] : culture;
    }

    private string GetText(string key, string fallback)
    {
        string value = _localizer.GetStringValue(key);

        return string.IsNullOrWhiteSpace(value) || string.Equals(value, key, StringComparison.OrdinalIgnoreCase)
            ? fallback
            : value;
    }

    private string GetExceptionMessage(Exception exception)
    {
        if (string.IsNullOrWhiteSpace(exception.Message))
            return GetText("Common.GenericError", "İşlem sırasında bir hata oluştu.");

        return GetText(exception.Message, exception.Message);
    }
}
