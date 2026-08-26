using Core.Application.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture?}/payment-statuses")]
public sealed class PaymentStatusesController : Controller
{
    private const int DefaultPageIndex = 0;
    private const int DefaultPageSize = 50;

    private readonly IMediator _mediator;

    public PaymentStatusesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken, int page = DefaultPageIndex, int pageSize = DefaultPageSize)
    {
        var response = await _mediator.Send(new Symplify.BackOffice.Application.Features.PaymentStatuses.Queries.GetList.GetListPaymentStatusQuery
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

        var response = await _mediator.Send(new Symplify.BackOffice.Application.Features.PaymentStatuses.Queries.GetById.GetByIdPaymentStatusQuery
        {
            Id = id
        }, cancellationToken);

        return View(response);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return View(new Symplify.BackOffice.Application.Features.PaymentStatuses.Commands.Create.CreatePaymentStatusCommand());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Symplify.BackOffice.Application.Features.PaymentStatuses.Commands.Create.CreatePaymentStatusCommand command, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(command);

        await _mediator.Send(command, cancellationToken);

        TempData["SuccessMessage"] = "Payment Statuses kaydı oluşturuldu.";
        return RedirectToIndex();
    }

    [HttpGet("edit/{id}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
            return BadRequest();

        var response = await _mediator.Send(new Symplify.BackOffice.Application.Features.PaymentStatuses.Queries.GetById.GetByIdPaymentStatusQuery
        {
            Id = id
        }, cancellationToken);

        Symplify.BackOffice.Application.Features.PaymentStatuses.Commands.Update.UpdatePaymentStatusCommand command = new()
        {
            Id = response.Id,
            Code = response.Code,
            Order = response.Order,
            IsActive = response.IsActive,
        };

        return View(command);
    }

    [HttpPost("edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Symplify.BackOffice.Application.Features.PaymentStatuses.Commands.Update.UpdatePaymentStatusCommand command, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(command);

        await _mediator.Send(command, cancellationToken);

        TempData["SuccessMessage"] = "Payment Statuses kaydı güncellendi.";
        return RedirectToIndex();
    }

    [HttpPost("delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Symplify.BackOffice.Application.Features.PaymentStatuses.Commands.Delete.DeletePaymentStatusCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);

        TempData["SuccessMessage"] = "Payment Statuses kaydı silindi.";
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
