using Core.Application.Requests;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.Application.Features.OrganizationApiKeys.Commands.Create;
using Symplify.BackOffice.Application.Features.OrganizationApiKeys.Constants;
using Symplify.BackOffice.Application.Features.OrganizationApiKeys.Queries.GetList;
using Symplify.BackOffice.Application.Features.Organizations.Queries.GetById;
using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.WebUI.Models.OrganizationApiKeys;
using Symplify.BackOffice.WebUI.Models.Shared.DataTables;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture=tr-TR}/[controller]/[action]")]
public sealed class OrganizationApiKeysController : Controller
{
    private readonly IMediator _mediator;
    private readonly IBackOfficeViewLocalizer _localizer;

    public OrganizationApiKeysController(IMediator mediator, IBackOfficeViewLocalizer localizer)
    {
        _mediator = mediator;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid organizationId, CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
        {
            TempData["ErrorMessage"] = GetText(
                "BackOffice.OrganizationApiKeys.Validation.OrganizationRequired",
                "API key yönetimi için önce bir organizasyon seçmelisiniz.");

            return RedirectToOrganizationsIndex();
        }

        try
        {
            var organization = await _mediator.Send(new GetByIdOrganizationQuery { Id = organizationId }, cancellationToken);

            return View(new OrganizationApiKeysIndexViewModel
            {
                OrganizationId = organization.Id,
                OrganizationName = organization.Name,
                OrganizationCode = organization.Code,
                IsOrganizationActive = organization.IsActive,
                OneTimePlainTextKey = TempData["OneTimeApiKey"]?.ToString()
            });
        }
        catch (BusinessException)
        {
            TempData["ErrorMessage"] = GetText(
                "BackOffice.OrganizationApiKeys.Validation.OrganizationNotFound",
                "Seçilen organizasyon bulunamadı. API key yönetimi için listeden geçerli bir organizasyon seçin.");

            return RedirectToOrganizationsIndex();
        }
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GetList(
        [FromForm] DataTableRequest request,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
        {
            return Json(new
            {
                draw = request.Draw,
                recordsTotal = 0,
                recordsFiltered = 0,
                data = Array.Empty<object>()
            });
        }

        DataTableQueryOptions options = DataTableQueryOptions.From(
            request,
            "createdDate",
            "desc",
            new[] { "name", "environment", "keyType", "isActive", "lastUsedAt", "createdDate" });

        var response = await _mediator.Send(
            new GetListOrganizationApiKeyQuery
            {
                OrganizationId = organizationId,
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
                organizationId = item.OrganizationId,
                name = item.Name,
                environment = item.Environment,
                keyType = item.KeyType,
                keyPrefix = item.KeyPrefix,
                description = item.Description,
                scopes = item.Scopes,
                allowedIpAddresses = item.AllowedIpAddresses,
                allowedDomains = item.AllowedDomains,
                isActive = item.IsActive,
                expiresAt = item.ExpiresAt?.ToLocalTime().ToString("dd.MM.yyyy"),
                lastUsedAt = item.LastUsedAt?.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                revokedAt = item.RevokedAt?.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                createdDate = item.CreatedDate.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
            })
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid organizationId, CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
        {
            TempData["ErrorMessage"] = GetText(
                "BackOffice.OrganizationApiKeys.Validation.OrganizationRequired",
                "API key oluşturmak için önce bir organizasyon seçmelisiniz.");

            return RedirectToOrganizationsIndex();
        }

        try
        {
            var organization = await _mediator.Send(new GetByIdOrganizationQuery { Id = organizationId }, cancellationToken);

            return View(new CreateOrganizationApiKeyViewModel
            {
                OrganizationId = organization.Id,
                OrganizationName = organization.Name,
                OrganizationCode = organization.Code,
                IsOrganizationActive = organization.IsActive,
                IsActive = true,
                AvailableScopes = BuildScopes(new[]
                {
                    OrganizationApiKeyScopes.CongressRead,
                    OrganizationApiKeyScopes.SubmissionRead
                })
            });
        }
        catch (BusinessException)
        {
            TempData["ErrorMessage"] = GetText(
                "BackOffice.OrganizationApiKeys.Validation.OrganizationNotFound",
                "Seçilen organizasyon bulunamadı. API key oluşturmak için listeden geçerli bir organizasyon seçin.");

            return RedirectToOrganizationsIndex();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOrganizationApiKeyViewModel model, CancellationToken cancellationToken)
    {
        NormalizeCreateModelState(model);

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
            CreatedOrganizationApiKeyResponse response = await _mediator.Send(
                new CreateOrganizationApiKeyCommand
                {
                    OrganizationId = model.OrganizationId,
                    Name = model.Name!.Trim(),
                    Environment = model.Environment,
                    KeyType = model.KeyType,
                    ExpiresAt = model.ExpiresAt,
                    Description = model.Description,
                    Scopes = NormalizeScopes(model.Scopes),
                    AllowedIpAddresses = model.AllowedIpAddresses,
                    AllowedDomains = model.AllowedDomains,
                    IsActive = model.IsActive
                },
                cancellationToken);

            return Ok(new
            {
                success = true,
                message = GetText(
                    "BackOffice.OrganizationApiKeys.Messages.Created",
                    "API key oluşturuldu."),
                redirectUrl = Url.Action(nameof(Index), new
                {
                    culture = CurrentCulture(),
                    organizationId = model.OrganizationId
                }),
                oneTimeApiKey = response.PlainTextKey
            });
        }
        catch (BusinessException exception)
        {
            return BadRequest(new
            {
                success = false,
                message = GetText(exception.Message, exception.Message)
            });
        }
    }

    private void NormalizeCreateModelState(CreateOrganizationApiKeyViewModel model)
    {
        if (model.OrganizationId == Guid.Empty)
        {
            ReplaceModelStateError(
                nameof(model.OrganizationId),
                GetText(
                    "BackOffice.OrganizationApiKeys.Validation.OrganizationRequired",
                    "API key oluşturmak için önce bir organizasyon seçmelisiniz."));
        }

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ReplaceModelStateError(
                nameof(model.Name),
                GetText(
                    "BackOffice.OrganizationApiKeys.Validation.NameRequired",
                    "API key adı zorunludur."));
        }
        else
        {
            ReplaceModelStateErrorIfInvalid(
                nameof(model.Name),
                GetText(
                    "BackOffice.OrganizationApiKeys.Validation.NameRequired",
                    "API key adı zorunludur."));
        }

        ReplaceModelStateErrorIfInvalid(
            nameof(model.Environment),
            GetText(
                "BackOffice.OrganizationApiKeys.Validation.EnvironmentRequired",
                "Ortam bilgisi zorunludur."));

        ReplaceModelStateErrorIfInvalid(
            nameof(model.KeyType),
            GetText(
                "BackOffice.OrganizationApiKeys.Validation.KeyTypeRequired",
                "Anahtar tipi zorunludur."));

        if (model.Scopes == null || model.Scopes.All(string.IsNullOrWhiteSpace))
        {
            ReplaceModelStateError(
                nameof(model.Scopes),
                GetText(
                    "BackOffice.OrganizationApiKeys.Validation.ScopeRequired",
                    "En az bir yetki kapsamı seçmelisiniz."));
        }
        else
        {
            ReplaceModelStateErrorIfInvalid(
                nameof(model.Scopes),
                GetText(
                    "BackOffice.OrganizationApiKeys.Validation.ScopeRequired",
                    "En az bir yetki kapsamı seçmelisiniz."));
        }
    }

    private static List<string> NormalizeScopes(IEnumerable<string>? scopes)
    {
        return scopes?
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Select(scope => scope.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();
    }

    private Dictionary<string, string[]> GetModelStateErrors()
    {
        return ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => NormalizeModelStateKey(entry.Key),
                entry => entry.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? GetText("Common.InvalidField", "Alan geçersiz.")
                        : error.ErrorMessage)
                    .ToArray());
    }

    private static string NormalizeModelStateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return key;
        }

        int lastDotIndex = key.LastIndexOf('.');
        return lastDotIndex >= 0 && lastDotIndex < key.Length - 1
            ? key[(lastDotIndex + 1)..]
            : key;
    }

    private void ReplaceModelStateErrorIfInvalid(string key, string message)
    {
        if (!ModelState.TryGetValue(key, out var state))
        {
            return;
        }

        if (state.Errors.Count == 0)
        {
            return;
        }

        ReplaceModelStateError(key, message);
    }

    private void ReplaceModelStateError(string key, string message)
    {
        ModelState.Remove(key);
        ModelState.AddModelError(key, message);
    }

    private IActionResult RedirectToOrganizationsIndex()
    {
        return RedirectToAction("Index", "Organizations", new { culture = CurrentCulture() });
    }

    private string CurrentCulture()
    {
        return RouteData.Values["culture"]?.ToString() ?? "tr-TR";
    }

    private List<OrganizationApiKeyScopeViewModel> BuildScopes(IEnumerable<string>? selectedScopes)
    {
        HashSet<string> selected = NormalizeScopes(selectedScopes).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new List<OrganizationApiKeyScopeViewModel>
        {
            CreateScope(OrganizationApiKeyScopes.CongressRead, "CongressRead", selected),
            CreateScope(OrganizationApiKeyScopes.SubmissionRead, "SubmissionRead", selected),
            CreateScope(OrganizationApiKeyScopes.SubmissionWrite, "SubmissionWrite", selected),
            CreateScope(OrganizationApiKeyScopes.PaymentWrite, "PaymentWrite", selected),
            CreateScope(OrganizationApiKeyScopes.UserRead, "UserRead", selected),
            CreateScope(OrganizationApiKeyScopes.WebhookSend, "WebhookSend", selected)
        };
    }

    private OrganizationApiKeyScopeViewModel CreateScope(string value, string keySuffix, HashSet<string> selected)
    {
        return new OrganizationApiKeyScopeViewModel
        {
            Value = value,
            Title = GetText($"BackOffice.OrganizationApiKeys.Scopes.{keySuffix}.Title", value),
            Description = GetText($"BackOffice.OrganizationApiKeys.Scopes.{keySuffix}.Description", string.Empty),
            IsSelected = selected.Contains(value)
        };
    }

    private string GetText(string key, string fallback)
    {
        string value = _localizer.GetStringValue(key);
        return string.IsNullOrWhiteSpace(value) || string.Equals(value, key, StringComparison.OrdinalIgnoreCase)
            ? fallback
            : value;
    }
}
