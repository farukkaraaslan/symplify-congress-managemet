using System.Security.Claims;
using Core.Application.Requests;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.Application.Features.ReviewerEvaluations.Commands.Save;
using Symplify.BackOffice.Application.Features.ReviewerEvaluations.Constants;
using Symplify.BackOffice.Application.Features.ReviewerEvaluations.Queries.GetForm;
using Symplify.BackOffice.Application.Features.ReviewerEvaluations.Queries.GetList;
using Symplify.BackOffice.WebUI.Models.ReviewerEvaluations;
using Symplify.BackOffice.WebUI.Models.Shared.DataTables;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture?}/reviewer-evaluations")]
public sealed class ReviewerEvaluationsController : Controller
{
    private const int StatsPageSize = 100000;

    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "pending",
        "inProgress",
        "completed",
        "dueSoon",
        "overdue"
    };

    private readonly IMediator _mediator;

    public ReviewerEvaluationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? searchText,
        Guid? congressId,
        string? status,
        Guid? topicId,
        Guid? submissionTypeId,
        CancellationToken cancellationToken = default)
    {
        string? culture = RouteData.Values["culture"]?.ToString();
        Guid? currentUserId = GetCurrentUserId();

        GetReviewerEvaluationListResponse response = await _mediator.Send(new GetReviewerEvaluationListQuery
        {
            CurrentUserId = currentUserId,
            Culture = culture,
            PageRequest = new PageRequest
            {
                Page = 0,
                PageSize = StatsPageSize
            },
            SearchText = NormalizeSearchText(searchText),
            CongressId = NormalizeGuid(congressId),
            Status = NormalizeStatus(status),
            TopicId = NormalizeGuid(topicId),
            SubmissionTypeId = NormalizeGuid(submissionTypeId),
            SortColumn = "dueDate",
            SortDirection = "asc"
        }, cancellationToken);

        GetReviewerEvaluationListResponse filterSource = await _mediator.Send(new GetReviewerEvaluationListQuery
        {
            CurrentUserId = currentUserId,
            Culture = culture,
            PageRequest = new PageRequest
            {
                Page = 0,
                PageSize = StatsPageSize
            },
            SortColumn = "dueDate",
            SortDirection = "asc"
        }, cancellationToken);

        return View(new ReviewerEvaluationIndexViewModel
        {
            Evaluations = response,
            FilterOptions = BuildFilterOptions(filterSource.Items),
            SearchText = NormalizeSearchText(searchText),
            CongressId = NormalizeGuid(congressId),
            Status = NormalizeStatus(status),
            TopicId = NormalizeGuid(topicId),
            SubmissionTypeId = NormalizeGuid(submissionTypeId)
        });
    }

    [HttpPost("get-list")]
    [HttpPost("GetList")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GetList(
        [FromForm] DataTableRequest request,
        [FromForm] string? searchText,
        [FromForm] Guid? congressId,
        [FromForm] string? status,
        [FromForm] Guid? topicId,
        [FromForm] Guid? submissionTypeId,
        CancellationToken cancellationToken)
    {
        DataTableQueryOptions tableOptions = DataTableQueryOptions.From(
            request,
            defaultSortColumn: "dueDate",
            defaultSortDirection: "asc",
            allowedSortColumns: new[]
            {
                "submissionNumber",
                "type",
                "title",
                "topic",
                "congress",
                "assignedDate",
                "dueDate",
                "status",
                "recommendation"
            });

        string? effectiveSearchText = NormalizeSearchText(searchText) ?? tableOptions.SearchText;
        Guid? normalizedCongressId = NormalizeGuid(congressId);
        string? normalizedStatus = NormalizeStatus(status);
        Guid? normalizedTopicId = NormalizeGuid(topicId);
        Guid? normalizedSubmissionTypeId = NormalizeGuid(submissionTypeId);
        string? culture = RouteData.Values["culture"]?.ToString();
        Guid? currentUserId = GetCurrentUserId();

        GetReviewerEvaluationListResponse response = await _mediator.Send(new GetReviewerEvaluationListQuery
        {
            CurrentUserId = currentUserId,
            Culture = culture,
            PageRequest = new PageRequest
            {
                Page = tableOptions.Page,
                PageSize = tableOptions.PageSize
            },
            SearchText = effectiveSearchText,
            CongressId = normalizedCongressId,
            Status = normalizedStatus,
            TopicId = normalizedTopicId,
            SubmissionTypeId = normalizedSubmissionTypeId,
            SortColumn = tableOptions.SortColumn,
            SortDirection = tableOptions.SortDirection
        }, cancellationToken);

        List<object> pageItems = response.Items
            .Select((item, index) => ToDataTableRow(item, tableOptions.Start + index + 1))
            .Cast<object>()
            .ToList();

        GetReviewerEvaluationListResponse statsResponse = await _mediator.Send(new GetReviewerEvaluationListQuery
        {
            CurrentUserId = currentUserId,
            Culture = culture,
            PageRequest = new PageRequest
            {
                Page = 0,
                PageSize = StatsPageSize
            },
            SearchText = effectiveSearchText,
            CongressId = normalizedCongressId,
            Status = normalizedStatus,
            TopicId = normalizedTopicId,
            SubmissionTypeId = normalizedSubmissionTypeId,
            SortColumn = tableOptions.SortColumn,
            SortDirection = tableOptions.SortDirection
        }, cancellationToken);

        return Json(new
        {
            draw = request.Draw,
            recordsTotal = response.Count,
            recordsFiltered = response.Count,
            data = pageItems,
            stats = BuildStats(statsResponse.Items)
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Evaluate(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest();

        GetReviewerEvaluationFormResponse response = await _mediator.Send(new GetReviewerEvaluationFormQuery
        {
            EvaluationId = id,
            CurrentUserId = GetCurrentUserId(),
            Culture = RouteData.Values["culture"]?.ToString()
        }, cancellationToken);

        return View(response);
    }

    [HttpPost("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Evaluate(Guid id, SaveReviewerEvaluationCommand command, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest();

        command.EvaluationId = id;
        command.CurrentUserId = GetCurrentUserId();

        try
        {
            SavedReviewerEvaluationResponse response = await _mediator.Send(command, cancellationToken);

            TempData["SuccessMessage"] = ReviewerEvaluationResourceKeys.MessageSubmitted;

            string? culture = RouteData.Values["culture"]?.ToString();
            return RedirectToAction(nameof(Index), new { culture });
        }
        catch (BusinessException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
            return RedirectToAction(nameof(Evaluate), new { culture = RouteData.Values["culture"]?.ToString(), id });
        }
    }

    private ReviewerEvaluationFilterOptionsViewModel BuildFilterOptions(IEnumerable<GetReviewerEvaluationListItemDto>? items)
    {
        List<GetReviewerEvaluationListItemDto> rows = items?.ToList() ?? new List<GetReviewerEvaluationListItemDto>();

        return new ReviewerEvaluationFilterOptionsViewModel
        {
            Congresses = rows
                .Where(item => item.CongressId != Guid.Empty)
                .GroupBy(item => item.CongressId)
                .Select(group => new ReviewerEvaluationFilterOptionViewModel
                {
                    Value = group.Key.ToString(),
                    Text = group.Select(item => item.CongressName).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? group.Key.ToString()
                })
                .OrderBy(item => item.Text)
                .ToList(),

            Statuses = BuildStatusOptions(),

            Topics = rows
                .Where(item => item.TopicId.HasValue && item.TopicId.Value != Guid.Empty)
                .GroupBy(item => item.TopicId!.Value)
                .Select(group => new ReviewerEvaluationFilterOptionViewModel
                {
                    Value = group.Key.ToString(),
                    Text = group.Select(item => item.TopicName).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? group.Key.ToString()
                })
                .OrderBy(item => item.Text)
                .ToList(),

            SubmissionTypes = rows
                .Where(item => item.SubmissionTypeId.HasValue && item.SubmissionTypeId.Value != Guid.Empty)
                .GroupBy(item => item.SubmissionTypeId!.Value)
                .Select(group => new ReviewerEvaluationFilterOptionViewModel
                {
                    Value = group.Key.ToString(),
                    Text = group.Select(item => item.SubmissionTypeName).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? group.Key.ToString()
                })
                .OrderBy(item => item.Text)
                .ToList()
        };
    }

    private static IReadOnlyList<ReviewerEvaluationFilterOptionViewModel> BuildStatusOptions()
    {
        return new List<ReviewerEvaluationFilterOptionViewModel>
        {
            new() { Value = "pending", Text = ReviewerEvaluationResourceKeys.FilterStatusPending },
            new() { Value = "inProgress", Text = ReviewerEvaluationResourceKeys.FilterStatusInProgress },
            new() { Value = "completed", Text = ReviewerEvaluationResourceKeys.FilterStatusCompleted },
            new() { Value = "dueSoon", Text = ReviewerEvaluationResourceKeys.FilterStatusDueSoon },
            new() { Value = "overdue", Text = ReviewerEvaluationResourceKeys.FilterStatusOverdue }
        };
    }

    private static object ToDataTableRow(GetReviewerEvaluationListItemDto item, int rowNumber)
    {
        return new
        {
            rowNumber,
            evaluationId = item.EvaluationId,
            submissionId = item.SubmissionId,
            congressId = item.CongressId,
            submissionTypeId = item.SubmissionTypeId,
            topicId = item.TopicId,
            submissionNumber = item.SubmissionNumber,
            title = item.Title,
            titleEn = item.TitleEn,
            submissionTypeName = item.SubmissionTypeName,
            topicName = item.TopicName,
            congressName = item.CongressName,
            assignedDate = FormatDate(item.AssignedDate),
            assignedTime = FormatTime(item.AssignedDate),
            dueDate = FormatDate(item.DueDate),
            dueTextKey = ResolveDueTextKey(item),
            daysRemaining = item.DaysRemaining,
            statusText = item.StatusText,
            statusBadgeClass = item.StatusBadgeClass,
            recommendationText = item.RecommendationText,
            totalScore = item.TotalScore,
            completedAt = item.CompletedAt,
            isCompleted = item.IsCompleted,
            isOverdue = item.IsOverdue,
            isDueSoon = item.IsDueSoon,
            actionText = item.ActionText,
            actionIcon = item.ActionIcon
        };
    }

    private static object BuildStats(IEnumerable<GetReviewerEvaluationListItemDto>? items)
    {
        List<GetReviewerEvaluationListItemDto> rows = items?.ToList() ?? new List<GetReviewerEvaluationListItemDto>();

        return new
        {
            total = rows.Count,
            pending = rows.Count(item => !item.IsCompleted && !item.HasDraft),
            inProgress = rows.Count(item => !item.IsCompleted && item.HasDraft),
            completed = rows.Count(item => item.IsCompleted),
            dueSoon = rows.Count(item => !item.IsCompleted && item.IsDueSoon)
        };
    }

    private static string ResolveDueTextKey(GetReviewerEvaluationListItemDto item)
    {
        if (item.IsCompleted)
            return ReviewerEvaluationResourceKeys.StatusCompleted;

        if (item.IsOverdue)
            return ReviewerEvaluationResourceKeys.DueOverdue;

        if (item.DaysRemaining <= 0)
            return ReviewerEvaluationResourceKeys.DueToday;

        return ReviewerEvaluationResourceKeys.DueRemainingFormat;
    }

    private static string FormatDate(DateTime? value)
        => IsMeaningfulDate(value) ? value!.Value.ToString("dd.MM.yyyy") : "-";

    private static string FormatTime(DateTime? value)
        => IsMeaningfulDate(value) ? value!.Value.ToString("HH:mm") : "-";

    private static bool IsMeaningfulDate(DateTime? value)
        => value.HasValue && value.Value.Year >= 1900;

    private static Guid? NormalizeGuid(Guid? value)
    {
        return value.HasValue && value.Value != Guid.Empty ? value : null;
    }

    private static string? NormalizeStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        string trimmed = value.Trim();
        return AllowedStatuses.Contains(trimmed) ? trimmed : null;
    }

    private static string? NormalizeSearchText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private Guid? GetCurrentUserId()
    {
        string? rawId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(rawId, out Guid userId) ? userId : null;
    }
}
