using Core.Application.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture?}/authors")]
public sealed class AuthorsController : Controller
{
    private const int DefaultPageIndex = 0;
    private const int DefaultPageSize = 50;

    private readonly IMediator _mediator;

    public AuthorsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken, int page = DefaultPageIndex, int pageSize = DefaultPageSize)
    {
        var response = await _mediator.Send(new Symplify.BackOffice.Application.Features.Authors.Queries.GetList.GetListAuthorQuery
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

        var response = await _mediator.Send(new Symplify.BackOffice.Application.Features.Authors.Queries.GetById.GetByIdAuthorQuery
        {
            Id = id
        }, cancellationToken);

        return View(response);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return View(new Symplify.BackOffice.Application.Features.Authors.Commands.Create.CreateAuthorCommand());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Symplify.BackOffice.Application.Features.Authors.Commands.Create.CreateAuthorCommand command, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(command);

        await _mediator.Send(command, cancellationToken);

        TempData["SuccessMessage"] = "Authors kaydı oluşturuldu.";
        return RedirectToIndex();
    }

    [HttpGet("edit/{id}")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest();

        var response = await _mediator.Send(new Symplify.BackOffice.Application.Features.Authors.Queries.GetById.GetByIdAuthorQuery
        {
            Id = id
        }, cancellationToken);

        Symplify.BackOffice.Application.Features.Authors.Commands.Update.UpdateAuthorCommand command = new()
        {
            Id = response.Id,
            FirstName = response.FirstName,
            LastName = response.LastName,
            Email = response.Email,
            Institution = response.Institution,
            Orcid = response.Orcid,
            IsCorrespondingAuthor = response.IsCorrespondingAuthor,
        };

        return View(command);
    }

    [HttpPost("edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Symplify.BackOffice.Application.Features.Authors.Commands.Update.UpdateAuthorCommand command, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(command);

        await _mediator.Send(command, cancellationToken);

        TempData["SuccessMessage"] = "Authors kaydı güncellendi.";
        return RedirectToIndex();
    }

    [HttpPost("delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Symplify.BackOffice.Application.Features.Authors.Commands.Delete.DeleteAuthorCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);

        TempData["SuccessMessage"] = "Authors kaydı silindi.";
        return RedirectToIndex();
    }

    private RedirectToActionResult RedirectToIndex()
    {
        string? culture = RouteData.Values["culture"]?.ToString();

        if (string.IsNullOrWhiteSpace(culture))
            return RedirectToAction(nameof(Index));

        return RedirectToAction(nameof(Index), new { culture });
    }
}
