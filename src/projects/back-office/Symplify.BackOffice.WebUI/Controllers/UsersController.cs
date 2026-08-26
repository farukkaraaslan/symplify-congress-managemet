using Core.Application.Requests;
using Core.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Symplify.BackOffice.Application.Features.Auth.Queries.GetRegisterOptions;
using Symplify.BackOffice.Application.Features.Auth.Queries.GetStatesByCountry;
using Symplify.BackOffice.Application.Features.Congresses.Queries.GetList;
using Symplify.BackOffice.Application.Features.Countries.Queries.GetList;
using Symplify.BackOffice.Application.Features.Organizations.Queries.GetList;
using Symplify.BackOffice.Application.Features.Roles.Queries.GetList;
using Symplify.BackOffice.Application.Features.Titles.Queries.GetList;
using Symplify.BackOffice.Application.Features.Users.Commands.Create;
using Symplify.BackOffice.Application.Features.Users.Commands.Delete;
using Symplify.BackOffice.Application.Features.Users.Commands.ResetPassword;
using Symplify.BackOffice.Application.Features.Users.Commands.SetBlacklist;
using Symplify.BackOffice.Application.Features.Users.Commands.Update;
using Symplify.BackOffice.Application.Features.Users.Commands.UpdateClaims;
using Symplify.BackOffice.Application.Features.Users.Commands.UpdateRoles;
using Symplify.BackOffice.Application.Features.Users.Dtos;
using Symplify.BackOffice.Application.Features.Users.Queries.GetById;
using Symplify.BackOffice.Application.Features.Users.Queries.GetList;
using Symplify.BackOffice.WebUI.Models.Shared.DataTables;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture?}/users")]
public sealed class UsersController : Controller
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        ViewBag.OrganizationFilterOptions = await GetOrganizationOptionsAsync(null, cancellationToken);
        ViewBag.CongressFilterOptions = await GetAllCongressOptionsAsync(null, cancellationToken);
        ViewBag.CountryFilterOptions = await GetCountryOptionsAsync(null, cancellationToken);
        ViewBag.StateFilterOptions = await GetStateOptionsAsync(null, null, cancellationToken);
        ViewBag.RoleFilterOptions = await GetRoleOptionsAsync(cancellationToken);

        return View(new GetListResponse<UserListItemDto>
        {
            Items = new List<UserListItemDto>()
        });
    }

    [HttpPost("get-list")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GetList(
        [FromForm] DataTableRequest request,
        [FromForm] bool? isBlacklisted,
        [FromForm] Guid? organizationId,
        [FromForm] bool? emailConfirmed,
        [FromForm] Guid? countryId,
        [FromForm] Guid? stateId,
        [FromForm] Guid? congressId,
        [FromForm] string? roleName,
        [FromForm] string? accountStatus,
        CancellationToken cancellationToken)
    {
        DataTableQueryOptions tableOptions = DataTableQueryOptions.From(
            request,
            defaultSortColumn: "createdDate",
            defaultSortDirection: "desc",
            allowedSortColumns: new[]
            {
                "fullName",
                "email",
                "phoneNumber",
                "institution",
                "createdDate"
            });

        GetListResponse<UserListItemDto> response = await _mediator.Send(new GetListUserQuery
        {
            SearchText = tableOptions.SearchText,
            IsBlacklisted = isBlacklisted,
            OrganizationId = NormalizeOptionalGuid(organizationId),
            EmailConfirmed = emailConfirmed,
            CountryId = NormalizeOptionalGuid(countryId),
            StateId = NormalizeOptionalGuid(stateId),
            CongressId = NormalizeOptionalGuid(congressId),
            RoleName = NormalizeOptional(roleName),
            AccountStatus = NormalizeOptional(accountStatus),
            Culture = GetCurrentCulture(),
            SortColumn = tableOptions.SortColumn,
            SortDirection = tableOptions.SortDirection,
            PageRequest = new PageRequest
            {
                Page = tableOptions.Page,
                PageSize = tableOptions.PageSize
            }
        }, cancellationToken);

        List<object> pageItems = response.Items
            .Select((item, index) => ToDataTableRow(item, tableOptions.Start + index + 1))
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

    [HttpGet("details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        UserDetailDto detail = await _mediator.Send(new GetByIdUserQuery
        {
            Id = id,
            Culture = GetCurrentCulture()
        }, cancellationToken);

        return View(detail);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return View(new CreateUserCommand
        {
            GeneratePassword = true,
            EmailConfirmed = true
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserCommand command, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(command);

        CreatedUserDto created = await _mediator.Send(command, cancellationToken);

        TempData["SuccessMessage"] = "Kullanıcı oluşturuldu.";
        TempData["GeneratedPassword"] = created.GeneratedPassword;

        return RedirectToAction(nameof(Details), new { culture = GetCurrentCulture(), id = created.Id });
    }

    [HttpGet("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        UserDetailDto detail = await _mediator.Send(new GetByIdUserQuery
        {
            Id = id,
            Culture = GetCurrentCulture()
        }, cancellationToken);

        UpdateUserCommand command = new()
        {
            Id = detail.Id,
            Email = detail.Email,
            Name = detail.Name,
            Surname = detail.Surname,
            Institution = detail.Institution,
            TitleId = detail.TitleId,
            CountryId = detail.CountryId,
            StateId = detail.StateId,
            Orcid = detail.Orcid,
            PhoneNumber = detail.PhoneNumber,
            EmailConfirmed = detail.EmailConfirmed,
            LockoutEnabled = detail.LockoutEnabled,
            OrganizationAccessId = detail.OrganizationAccessId,
            OrganizationId = detail.OrganizationId,
            DefaultCongressId = detail.DefaultCongressId,
            OrganizationAccessIsActive = detail.OrganizationAccessIsActive
        };

        ViewBag.UserDisplayName = detail.FullName;
        ViewBag.UserTitle = detail.TitleShortName;
        await PopulateEditOptionsAsync(command, cancellationToken);
        return View(command);
    }

    [HttpPost("edit/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UpdateUserCommand command, CancellationToken cancellationToken)
    {
        command.Id = id;

        if (!ModelState.IsValid)
        {
            await PopulateEditOptionsAsync(command, cancellationToken);
            return View(command);
        }

        UserDetailDto detail = await _mediator.Send(command, cancellationToken);

        TempData["SuccessMessage"] = "Kullanıcı güncellendi.";
        return RedirectToAction(nameof(Details), new { culture = GetCurrentCulture(), id = detail.Id });
    }

    [HttpGet("options/states")]
    public async Task<IActionResult> GetStateOptions(Guid? countryId, Guid? selectedId, CancellationToken cancellationToken)
    {
        List<SelectListItem> states = await GetStateOptionsAsync(selectedId, countryId, cancellationToken);
        return Json(states.Select(item => new
        {
            value = item.Value,
            text = item.Text,
            selected = item.Selected
        }));
    }

    [HttpGet("options/congresses")]
    public async Task<IActionResult> GetCongressOptions(Guid? organizationId, Guid? selectedId, CancellationToken cancellationToken)
    {
        List<SelectListItem> congresses = organizationId.HasValue
            ? await GetCongressOptionsAsync(selectedId, organizationId, cancellationToken)
            : await GetAllCongressOptionsAsync(selectedId, cancellationToken);

        return Json(congresses.Select(item => new
        {
            value = item.Value,
            text = item.Text,
            selected = item.Selected
        }));
    }

    [HttpPost("reset-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(Guid userId, CancellationToken cancellationToken)
    {
        ResetUserPasswordDto result = await _mediator.Send(new ResetUserPasswordCommand
        {
            UserId = userId
        }, cancellationToken);

        TempData["SuccessMessage"] = "Kullanıcı şifresi sıfırlandı.";
        TempData["GeneratedPassword"] = result.GeneratedPassword;
        TempData["RemainingPasswordResetAttempts"] = result.RemainingAttemptsInWindow;

        return RedirectToAction(nameof(Details), new { culture = GetCurrentCulture(), id = userId });
    }

    [HttpPost("roles")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRoles(UpdateUserRolesCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        TempData["SuccessMessage"] = "Kullanıcı rolleri güncellendi.";
        return RedirectToAction(nameof(Details), new { culture = GetCurrentCulture(), id = command.UserId });
    }

    [HttpPost("claims")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateClaims(UpdateUserClaimsCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        TempData["SuccessMessage"] = "Kullanıcı yetkileri güncellendi.";
        return RedirectToAction(nameof(Details), new { culture = GetCurrentCulture(), id = command.UserId });
    }

    [HttpPost("blacklist")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetBlacklist(Guid userId, bool isBlacklisted, CancellationToken cancellationToken)
    {
        await _mediator.Send(new SetUserBlacklistCommand
        {
            UserId = userId,
            IsBlacklisted = isBlacklisted
        }, cancellationToken);

        TempData["SuccessMessage"] = isBlacklisted
            ? "Kullanıcı kara listeye alındı."
            : "Kullanıcı kara listeden çıkarıldı.";

        return RedirectToAction(nameof(Details), new { culture = GetCurrentCulture(), id = userId });
    }

    [HttpPost("delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid userId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteUserCommand { UserId = userId }, cancellationToken);
        TempData["SuccessMessage"] = "Kullanıcı pasife alındı.";
        return RedirectToAction(nameof(Index), new { culture = GetCurrentCulture() });
    }

    private async Task PopulateEditOptionsAsync(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        ViewBag.TitleOptions = await GetTitleOptionsAsync(command.TitleId, cancellationToken);
        ViewBag.CountryOptions = await GetCountryOptionsAsync(command.CountryId, cancellationToken);
        ViewBag.StateOptions = await GetStateOptionsAsync(command.StateId, command.CountryId, cancellationToken);
        ViewBag.OrganizationOptions = await GetOrganizationOptionsAsync(command.OrganizationId, cancellationToken);
        ViewBag.CongressOptions = await GetCongressOptionsAsync(command.DefaultCongressId, command.OrganizationId, cancellationToken);
    }

    private async Task<List<SelectListItem>> GetTitleOptionsAsync(Guid? selectedId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mediator.Send(new GetListTitleQuery
            {
                Culture = GetCurrentCulture(),
                IsActive = true,
                PageRequest = new PageRequest { Page = 0, PageSize = 500 },
                SortColumn = "order",
                SortDirection = "asc"
            }, cancellationToken);

            return response.Items
                .OrderBy(item => item.Order)
                .ThenBy(item => item.Name)
                .Select(item => new SelectListItem
                {
                    Value = item.Id.ToString(),
                    Text = string.IsNullOrWhiteSpace(item.Description) ? item.Name : item.Description,
                    Selected = selectedId.HasValue && item.Id == selectedId.Value
                })
                .ToList();
        }
        catch
        {
            return new List<SelectListItem>();
        }
    }

    private async Task<List<SelectListItem>> GetCountryOptionsAsync(Guid? selectedId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mediator.Send(new GetListCountryQuery
            {
                Culture = GetCurrentCulture(),
                IsActive = true,
                PageRequest = new PageRequest { Page = 0, PageSize = 500 }
            }, cancellationToken);

            return response.Items
                .OrderBy(item => item.Name)
                .Select(item => new SelectListItem
                {
                    Value = item.Id.ToString(),
                    Text = item.Name,
                    Selected = selectedId.HasValue && item.Id == selectedId.Value
                })
                .ToList();
        }
        catch
        {
            return new List<SelectListItem>();
        }
    }

    private async Task<List<SelectListItem>> GetStateOptionsAsync(
        Guid? selectedId,
        Guid? countryId,
        CancellationToken cancellationToken)
    {
        Guid? normalizedCountryId = NormalizeOptionalGuid(countryId);

        if (!normalizedCountryId.HasValue)
            return new List<SelectListItem>();

        // Kayıt ekranıyla aynı query ve aynı States/StateTranslations kaynağı kullanılır.
        List<AuthSelectOptionDto> states = await _mediator.Send(
            new GetStatesByCountryQuery
            {
                CountryId = normalizedCountryId.Value,
                Culture = GetCurrentCulture()
            },
            cancellationToken);

        return states
            .Select(item => new SelectListItem
            {
                Value = item.Value,
                Text = item.Text,
                Selected = selectedId.HasValue &&
                           Guid.TryParse(item.Value, out Guid stateId) &&
                           stateId == selectedId.Value
            })
            .ToList();
    }

    private async Task<List<SelectListItem>> GetOrganizationOptionsAsync(Guid? selectedId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mediator.Send(new GetListOrganizationQuery
            {
                PageRequest = new PageRequest { Page = 0, PageSize = 500 },
                SortColumn = "name",
                SortDirection = "asc"
            }, cancellationToken);

            return response.Items
                .Where(item => item.IsActive || (selectedId.HasValue && item.Id == selectedId.Value))
                .OrderBy(item => item.Name)
                .Select(item => new SelectListItem
                {
                    Value = item.Id.ToString(),
                    Text = string.IsNullOrWhiteSpace(item.ShortName) ? item.Name : $"{item.ShortName} - {item.Name}",
                    Selected = selectedId.HasValue && item.Id == selectedId.Value
                })
                .ToList();
        }
        catch
        {
            return new List<SelectListItem>();
        }
    }

    private async Task<List<SelectListItem>> GetRoleOptionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mediator.Send(new GetListRoleQuery
            {
                PageRequest = new PageRequest { Page = 0, PageSize = 500 }
            }, cancellationToken);

            return response.Items
                .OrderBy(item => item.Name)
                .Select(item => new SelectListItem
                {
                    Value = item.Name,
                    Text = item.Name
                })
                .ToList();
        }
        catch
        {
            return new List<SelectListItem>();
        }
    }

    private async Task<List<SelectListItem>> GetAllCongressOptionsAsync(Guid? selectedId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mediator.Send(new GetListCongressQuery
            {
                Culture = GetCurrentCulture(),
                PageRequest = new PageRequest { Page = 0, PageSize = 1000 },
                SortColumn = "startDate",
                SortDirection = "desc"
            }, cancellationToken);

            return response.Items
                .OrderByDescending(item => item.StartDate)
                .ThenBy(item => item.Title)
                .Select(item => new SelectListItem
                {
                    Value = item.Id.ToString(),
                    Text = string.IsNullOrWhiteSpace(item.Title) ? $"{item.Name} ({item.Code})" : $"{item.Title} ({item.Code})",
                    Selected = selectedId.HasValue && item.Id == selectedId.Value
                })
                .ToList();
        }
        catch
        {
            return new List<SelectListItem>();
        }
    }

    private async Task<List<SelectListItem>> GetCongressOptionsAsync(Guid? selectedId, Guid? organizationId, CancellationToken cancellationToken)
    {
        if (!organizationId.HasValue)
            return await GetAllCongressOptionsAsync(selectedId, cancellationToken);

        try
        {
            var response = await _mediator.Send(new GetListCongressQuery
            {
                Culture = GetCurrentCulture(),
                OrganizationId = organizationId.Value,
                PageRequest = new PageRequest { Page = 0, PageSize = 500 },
                SortColumn = "startDate",
                SortDirection = "desc"
            }, cancellationToken);

            return response.Items
                .OrderByDescending(item => item.StartDate)
                .ThenBy(item => item.Title)
                .Select(item => new SelectListItem
                {
                    Value = item.Id.ToString(),
                    Text = string.IsNullOrWhiteSpace(item.Title) ? $"{item.Name} ({item.Code})" : $"{item.Title} ({item.Code})",
                    Selected = selectedId.HasValue && item.Id == selectedId.Value
                })
                .ToList();
        }
        catch
        {
            return new List<SelectListItem>();
        }
    }

    private static object ToDataTableRow(UserListItemDto user, int rowNumber)
    {
        return new
        {
            rowNumber,
            id = user.Id,
            fullName = user.FullName,
            titleShortName = user.TitleShortName,
            email = user.Email,
            phoneNumber = user.PhoneNumber,
            institution = user.Institution,
            orcid = user.Orcid,
            countryName = user.CountryName,
            stateName = user.StateName,
            organizationName = user.OrganizationName,
            organizationShortName = user.OrganizationShortName,
            defaultCongressName = user.DefaultCongressName,
            rolesText = user.RolesText,
            emailConfirmed = user.EmailConfirmed,
            isBlacklisted = user.IsBlacklisted,
            isLockedOut = user.IsLockedOut,
            organizationAccessIsActive = user.OrganizationAccessIsActive,
            createdDate = user.CreatedDate.ToString("dd.MM.yyyy HH:mm")
        };
    }

    private static Guid? NormalizeOptionalGuid(Guid? value)
    {
        return value.HasValue && value.Value != Guid.Empty ? value.Value : null;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private string GetCurrentCulture()
    {
        string? routeCulture = RouteData.Values["culture"]?.ToString();
        return string.IsNullOrWhiteSpace(routeCulture) ? "tr-TR" : routeCulture;
    }
}
