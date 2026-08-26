using Core.Application.Requests;
using Core.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Features.Reviewers.Commands.Create;
using Symplify.BackOffice.Application.Features.Reviewers.Queries.GetList;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Identity;
using Symplify.BackOffice.WebUI.Models.Reviewers;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture?}/reviewers")]
public sealed class ReviewersController : Controller
{
    private const int DefaultPageIndex = 0;
    private const int DefaultPageSize = 50;
    private const int UserListLimit = 300;

    private readonly IMediator _mediator;
    private readonly UserManager<AppUser> _userManager;

    public ReviewersController(IMediator mediator, UserManager<AppUser> userManager)
    {
        _mediator = mediator;
        _userManager = userManager;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken, int page = DefaultPageIndex, int pageSize = DefaultPageSize)
    {
        var response = await _mediator.Send(new GetListReviewerQuery
        {
            PageRequest = new PageRequest
            {
                Page = page,
                PageSize = pageSize
            }
        }, cancellationToken);

        return View(response);
    }

    [HttpGet("users")]
    public async Task<IActionResult> Users(string? searchText, CancellationToken cancellationToken)
    {
        GetListResponse<GetListReviewerListItemDto> reviewerResponse = await _mediator.Send(new GetListReviewerQuery
        {
            PageRequest = new PageRequest
            {
                Page = 0,
                PageSize = 10000
            }
        }, cancellationToken);

        Dictionary<Guid, GetListReviewerListItemDto> reviewersByUserId = reviewerResponse.Items
            .GroupBy(item => item.UserId)
            .ToDictionary(group => group.Key, group => group.First());

        IQueryable<AppUser> query = _userManager.Users.AsNoTracking()
            .Where(user => user.DeletedDate == null);

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            string normalizedSearch = searchText.Trim().ToLower();
            query = query.Where(user =>
                (user.Name + " " + user.Surname).ToLower().Contains(normalizedSearch) ||
                (user.Email != null && user.Email.ToLower().Contains(normalizedSearch)) ||
                (user.Institution != null && user.Institution.ToLower().Contains(normalizedSearch)) ||
                (user.Orcid != null && user.Orcid.ToLower().Contains(normalizedSearch)));
        }

        List<AppUser> users = await query
            .OrderBy(user => user.Name)
            .ThenBy(user => user.Surname)
            .Take(UserListLimit)
            .ToListAsync(cancellationToken);

        ReviewerUserListViewModel model = new()
        {
            SearchText = searchText,
            Users = users.Select(user =>
            {
                reviewersByUserId.TryGetValue(user.Id, out GetListReviewerListItemDto? reviewer);

                return new ReviewerUserListItemViewModel
                {
                    UserId = user.Id,
                    FullName = JoinFullName(user.Name, user.Surname),
                    Email = user.Email,
                    Institution = user.Institution,
                    Orcid = user.Orcid,
                    IsBlacklisted = user.IsBlacklisted,
                    IsReviewer = reviewer is not null,
                    ReviewerId = reviewer?.Id,
                    ReviewerStatus = reviewer?.Status.ToString() ?? string.Empty
                };
            }).ToList()
        };

        return View(model);
    }

    [HttpPost("create-from-user")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFromUser(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            TempData["ErrorMessage"] = "Hakem yapılacak kullanıcı seçilmelidir.";
            return RedirectToUsers();
        }

        await _mediator.Send(new CreateReviewerCommand
        {
            UserId = userId,
            Status = ReviewerStatus.Accepted,
            IsActive = true
        }, cancellationToken);

        TempData["SuccessMessage"] = "Kullanıcı hakem havuzuna eklendi.";
        return RedirectToUsers();
    }

    [HttpGet("details/{id}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest();

        var response = await _mediator.Send(new Symplify.BackOffice.Application.Features.Reviewers.Queries.GetById.GetByIdReviewerQuery
        {
            Id = id
        }, cancellationToken);

        return View(response);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return View(new CreateReviewerCommand());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateReviewerCommand command, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(command);

        await _mediator.Send(command, cancellationToken);

        TempData["SuccessMessage"] = "Hakem kaydı oluşturuldu.";
        return RedirectToIndex();
    }

    [HttpGet("edit/{id}")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest();

        var response = await _mediator.Send(new Symplify.BackOffice.Application.Features.Reviewers.Queries.GetById.GetByIdReviewerQuery
        {
            Id = id
        }, cancellationToken);

        Symplify.BackOffice.Application.Features.Reviewers.Commands.Update.UpdateReviewerCommand command = new()
        {
            Id = response.Id,
            UserId = response.UserId,
            Status = response.Status,
            IsActive = response.IsActive,
        };

        return View(command);
    }

    [HttpPost("edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Symplify.BackOffice.Application.Features.Reviewers.Commands.Update.UpdateReviewerCommand command, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(command);

        await _mediator.Send(command, cancellationToken);

        TempData["SuccessMessage"] = "Hakem kaydı güncellendi.";
        return RedirectToIndex();
    }

    [HttpPost("delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Symplify.BackOffice.Application.Features.Reviewers.Commands.Delete.DeleteReviewerCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);

        TempData["SuccessMessage"] = "Hakem kaydı silindi.";
        return RedirectToIndex();
    }

    private RedirectToActionResult RedirectToIndex()
    {
        string? culture = RouteData.Values["culture"]?.ToString();
        return string.IsNullOrWhiteSpace(culture)
            ? RedirectToAction(nameof(Index))
            : RedirectToAction(nameof(Index), new { culture });
    }

    private RedirectToActionResult RedirectToUsers()
    {
        string? culture = RouteData.Values["culture"]?.ToString();
        return string.IsNullOrWhiteSpace(culture)
            ? RedirectToAction(nameof(Users))
            : RedirectToAction(nameof(Users), new { culture });
    }

    private static string JoinFullName(string? firstName, string? lastName)
    {
        string fullName = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? "-" : fullName;
    }
}
