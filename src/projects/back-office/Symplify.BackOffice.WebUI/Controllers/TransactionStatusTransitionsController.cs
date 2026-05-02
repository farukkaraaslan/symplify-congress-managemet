using Core.Application.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture?}/transaction-status-transitions")]
public sealed class TransactionStatusTransitionsController : Controller
{
    private const int DefaultPageIndex = 0;
    private const int DefaultPageSize = 50;

    private readonly IMediator _mediator;

    public TransactionStatusTransitionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken, int page = DefaultPageIndex, int pageSize = DefaultPageSize)
    {
        var response = await _mediator.Send(new Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Queries.GetList.GetListTransactionStatusTransitionQuery
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
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
            return BadRequest();

        var response = await _mediator.Send(new Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Queries.GetById.GetByIdTransactionStatusTransitionQuery
        {
            Id = id
        }, cancellationToken);

        return View(response);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return View(new Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Commands.Create.CreateTransactionStatusTransitionCommand());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Commands.Create.CreateTransactionStatusTransitionCommand command, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(command);

        await _mediator.Send(command, cancellationToken);

        TempData["SuccessMessage"] = "Transaction Status Transitions kaydı oluşturuldu.";
        return RedirectToIndex();
    }

    [HttpGet("edit/{id}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
            return BadRequest();

        var response = await _mediator.Send(new Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Queries.GetById.GetByIdTransactionStatusTransitionQuery
        {
            Id = id
        }, cancellationToken);

        Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Commands.Update.UpdateTransactionStatusTransitionCommand command = new()
        {
            Id = response.Id,
            FromStatusId = response.FromStatusId,
            ToStatusId = response.ToStatusId,
            IsActive = response.IsActive,
        };

        return View(command);
    }

    [HttpPost("edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Commands.Update.UpdateTransactionStatusTransitionCommand command, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(command);

        await _mediator.Send(command, cancellationToken);

        TempData["SuccessMessage"] = "Transaction Status Transitions kaydı güncellendi.";
        return RedirectToIndex();
    }

    [HttpPost("delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Commands.Delete.DeleteTransactionStatusTransitionCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);

        TempData["SuccessMessage"] = "Transaction Status Transitions kaydı silindi.";
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
