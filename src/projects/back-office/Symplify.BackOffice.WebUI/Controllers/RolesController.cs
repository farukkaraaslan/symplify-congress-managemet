using Core.Application.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.Application.Features.Roles.Commands.Create;
using Symplify.BackOffice.Application.Features.Roles.Commands.Delete;
using Symplify.BackOffice.Application.Features.Roles.Commands.Update;
using Symplify.BackOffice.Application.Features.Roles.Commands.UpdateClaims;
using Symplify.BackOffice.Application.Features.Roles.Dtos;
using Symplify.BackOffice.Application.Features.Roles.Queries.GetById;
using Symplify.BackOffice.Application.Features.Roles.Queries.GetList;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture?}/roles")]
public sealed class RolesController : Controller
{
    private const int DefaultPageIndex = 0;
    private const int DefaultPageSize = 25;

    private readonly IMediator _mediator;

    public RolesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? searchText,
        int page = DefaultPageIndex,
        int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new GetListRoleQuery
        {
            SearchText = searchText,
            PageRequest = new PageRequest
            {
                Page = page < 0 ? DefaultPageIndex : page,
                PageSize = pageSize <= 0 ? DefaultPageSize : pageSize
            }
        }, cancellationToken);

        return View(response);
    }

    [HttpGet("details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        RoleDetailDto detail = await _mediator.Send(new GetByIdRoleQuery { Id = id }, cancellationToken);
        return View(detail);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return View(new CreateRoleCommand());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(command);

        RoleDetailDto detail = await _mediator.Send(command, cancellationToken);
        TempData["SuccessMessage"] = "Rol oluşturuldu.";

        return RedirectToAction(nameof(Details), new { culture = GetCurrentCulture(), id = detail.Id });
    }

    [HttpGet("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        RoleDetailDto detail = await _mediator.Send(new GetByIdRoleQuery { Id = id }, cancellationToken);

        return View(new UpdateRoleCommand
        {
            Id = detail.Id,
            Name = detail.Name,
            Description = detail.Description
        });
    }

    [HttpPost("edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateRoleCommand command, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(command);

        RoleDetailDto detail = await _mediator.Send(command, cancellationToken);
        TempData["SuccessMessage"] = "Rol güncellendi.";

        return RedirectToAction(nameof(Details), new { culture = GetCurrentCulture(), id = detail.Id });
    }

    [HttpPost("claims")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateClaims(UpdateRoleClaimsCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        TempData["SuccessMessage"] = "Rol yetkileri güncellendi.";

        return RedirectToAction(nameof(Details), new { culture = GetCurrentCulture(), id = command.RoleId });
    }

    [HttpPost("delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid roleId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteRoleCommand { RoleId = roleId }, cancellationToken);
        TempData["SuccessMessage"] = "Rol pasife alındı.";

        return RedirectToAction(nameof(Index), new { culture = GetCurrentCulture() });
    }

    private string GetCurrentCulture()
    {
        string? routeCulture = RouteData.Values["culture"]?.ToString();
        return string.IsNullOrWhiteSpace(routeCulture) ? "tr-TR" : routeCulture;
    }
}
