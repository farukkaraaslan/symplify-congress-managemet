using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.Application.Features.ProgramManagement.Commands.DeleteBreak;
using Symplify.BackOffice.Application.Features.ProgramManagement.Commands.Generate;
using Symplify.BackOffice.Application.Features.ProgramManagement.Commands.MoveItem;
using Symplify.BackOffice.Application.Features.ProgramManagement.Commands.ReorderBreak;
using Symplify.BackOffice.Application.Features.ProgramManagement.Commands.ReorderItems;
using Symplify.BackOffice.Application.Features.ProgramManagement.Commands.Reset;
using Symplify.BackOffice.Application.Features.ProgramManagement.Commands.ToggleItemLock;
using Symplify.BackOffice.Application.Features.ProgramManagement.Commands.UpdateItemDuration;
using Symplify.BackOffice.Application.Features.ProgramManagement.Commands.UpdateSessionOfficials;
using Symplify.BackOffice.Application.Features.ProgramManagement.Models;
using Symplify.BackOffice.Application.Features.ProgramManagement.Queries.GetDraftPdf;
using Symplify.BackOffice.Application.Features.ProgramManagement.Queries.GetDraftWord;
using Symplify.BackOffice.Application.Features.ProgramManagement.Queries.GetPage;
using Symplify.BackOffice.Application.Services.Urls;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.WebUI.Models.ProgramManagement;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture?}/program-management")]
public sealed class ProgramManagementController : Controller
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProgramManagementController> _logger;
    private readonly IPublicUrlService _publicUrlService;

    public ProgramManagementController(
        IMediator mediator,
        ILogger<ProgramManagementController> logger,
        IPublicUrlService publicUrlService)
    {
        _mediator = mediator;
        _logger = logger;
        _publicUrlService = publicUrlService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(Guid? congressId, CancellationToken cancellationToken)
    {
        ProgramManagementPageResponse page = await _mediator.Send(new GetProgramManagementPageQuery
        {
            CongressId = congressId,
            Culture = RouteData.Values["culture"]?.ToString()
        }, cancellationToken);

        GenerateProgramViewModel generate = BuildGenerateModel(page);
        return View(new ProgramManagementIndexViewModel
        {
            Page = page,
            Generate = generate,
            Export = new ProgramBookExportViewModel
            {
                CongressId = page.SelectedCongressId ?? Guid.Empty
            }
        });
    }

    [HttpPost("generate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(
        [Bind(Prefix = "Generate")] GenerateProgramViewModel model,
        CancellationToken cancellationToken)
    {
        string? culture = RouteData.Values["culture"]?.ToString();
        if (!ModelState.IsValid)
        {
            string validationMessage = string.Join(" ", ModelState.Values
                .SelectMany(x => x.Errors)
                .Select(x => x.ErrorMessage)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.Ordinal));

            TempData["ErrorMessage"] = string.IsNullOrWhiteSpace(validationMessage)
                ? "Program ayarları geçerli değil. Lütfen süre ve saat alanlarını kontrol edin."
                : validationMessage;
            return RedirectToAction(nameof(Index), new { culture, congressId = model.CongressId });
        }

        try
        {
            ProgramGenerationResult result = await _mediator.Send(new GenerateCongressProgramCommand
            {
                CongressId = model.CongressId,
                RoomIds = model.RoomIds,
                DayStartTime = model.DayStartTime,
                DayEndTime = model.DayEndTime,
                SessionDurationMinutes = model.SessionDurationMinutes,
                PresentationDurationMinutes = model.PresentationDurationMinutes,
                QuestionAnswerDurationMinutes = model.IncludeQuestionAnswer
                    ? model.QuestionAnswerDurationMinutes
                    : 0,
                BreakDurationMinutes = model.BreakDurationMinutes,
                IncludeSessionBreaks = model.IncludeSessionBreaks,
                SessionBreakDurationMinutes = model.IncludeSessionBreaks
                    ? model.SessionBreakDurationMinutes
                    : 0,
                IncludeOpening = model.IncludeOpening,
                OpeningDurationMinutes = model.OpeningDurationMinutes,
                OpeningTitle = model.OpeningTitle,
                OpeningRoomId = model.OpeningRoomId,
                IncludeLunch = model.IncludeLunch,
                LunchStartTime = model.LunchStartTime,
                LunchDurationMinutes = model.LunchDurationMinutes,
                LunchTitle = model.LunchTitle,
                Mode = model.Mode,
                SubmissionScopePreset = model.SubmissionScopePreset,
                WorkflowStatusCodes = model.WorkflowStatusCodes,
                PaymentStatusIds = model.PaymentStatusIds,
                SubmissionTypeIds = model.SubmissionTypeIds,
                TopicIds = model.TopicIds,
                SubmissionSearchText = model.SubmissionSearchText,
                PerformedByUserId = GetCurrentUserId(),
                Culture = culture
            }, cancellationToken);

            TempData["SuccessMessage"] = model.Mode == CongressProgramGenerationMode.FillUnassigned
                ? $"Atanmamış bildiriler yerleştirildi. Toplam {result.AssignedSubmissionCount} bildiri programda, {result.UnassignedSubmissionCount} bildiri dışarıda kaldı."
                : $"Program taslağı oluşturuldu. {result.AssignedSubmissionCount}/{result.EligibleSubmissionCount} bildiri, {result.SessionCount} oturuma yerleştirildi.";
        }
        catch (Exception exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Index), new { culture, congressId = model.CongressId });
    }

    [HttpPost("reorder-items")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReorderItems(
        [FromBody] ReorderItemsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new ReorderProgramItemsCommand
            {
                CongressId = request.CongressId,
                MovedItemId = request.MovedItemId,
                TargetSessionId = request.TargetSessionId,
                OrderedItemIds = request.OrderedItemIds
            }, cancellationToken);
            return Json(new { success = true });
        }
        catch (Exception exception)
        {
            return BadRequest(new { success = false, message = exception.Message });
        }
    }

    [HttpPost("reorder-break")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReorderBreak(
        [FromBody] ReorderBreakRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new ReorderProgramBreakCommand
            {
                CongressId = request.CongressId,
                ProgramDayId = request.ProgramDayId,
                EventRoomId = request.EventRoomId,
                BreakId = request.BreakId,
                TargetSessionId = request.TargetSessionId,
                TargetItemIndex = request.TargetItemIndex,
                OrderedBlockKeys = request.OrderedBlockKeys
            }, cancellationToken);
            return Json(new { success = true });
        }
        catch (Exception exception)
        {
            return BadRequest(new { success = false, message = exception.Message });
        }
    }

    [HttpPost("delete-break")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBreak(
        [FromBody] DeleteBreakRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new DeleteProgramBreakCommand
            {
                CongressId = request.CongressId,
                BreakId = request.BreakId
            }, cancellationToken);
            return Json(new { success = true });
        }
        catch (Exception exception)
        {
            return BadRequest(new { success = false, message = exception.Message });
        }
    }

    [HttpPost("update-session-officials")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSessionOfficials(
        [FromBody] UpdateSessionOfficialsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new UpdateSessionOfficialsCommand
            {
                CongressId = request.CongressId,
                SessionId = request.SessionId,
                ChairAuthorId = request.ChairAuthorId,
                ChairBoardMemberId = request.ChairBoardMemberId,
                ViceChairAuthorId = request.ViceChairAuthorId,
                ViceChairBoardMemberId = request.ViceChairBoardMemberId
            }, cancellationToken);
            return Json(new { success = true });
        }
        catch (Exception exception)
        {
            return BadRequest(new { success = false, message = exception.Message });
        }
    }

    [HttpPost("move-item")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveItem(
        [FromBody] MoveItemRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new MoveProgramItemCommand
            {
                CongressId = request.CongressId,
                ItemId = request.ItemId,
                TargetSessionId = request.TargetSessionId
            }, cancellationToken);
            return Json(new { success = true });
        }
        catch (Exception exception)
        {
            return BadRequest(new { success = false, message = exception.Message });
        }
    }

    [HttpPost("update-duration")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDuration(
        [FromBody] UpdateDurationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new UpdateProgramItemDurationCommand
            {
                CongressId = request.CongressId,
                ItemId = request.ItemId,
                DurationMinutes = request.DurationMinutes
            }, cancellationToken);
            return Json(new { success = true });
        }
        catch (Exception exception)
        {
            return BadRequest(new { success = false, message = exception.Message });
        }
    }

    [HttpPost("toggle-lock")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(
        [FromBody] ToggleLockRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            bool isLocked = await _mediator.Send(new ToggleProgramItemLockCommand
            {
                CongressId = request.CongressId,
                ItemId = request.ItemId
            }, cancellationToken);
            return Json(new { success = true, isLocked });
        }
        catch (Exception exception)
        {
            return BadRequest(new { success = false, message = exception.Message });
        }
    }

    [HttpPost("draft-pdf")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DraftPdf(
        [Bind(Prefix = "Export")] ProgramBookExportViewModel model,
        [FromForm(Name = "coverImageSelected")] bool coverImageSelected,
        [FromForm(Name = "Export.CoverImageFile")] IFormFile? explicitlyBoundCoverImage,
        CancellationToken cancellationToken)
    {
        if (model.CongressId == Guid.Empty)
            return BadRequest("Kongre seçimi zorunludur.");

        try
        {
            ProgramBookCoverDto cover = await ReadCoverAsync(
                model,
                coverImageSelected,
                explicitlyBoundCoverImage,
                cancellationToken);
            string? culture = RouteData.Values["culture"]?.ToString();
            ProgramDraftPdfResponse response = await _mediator.Send(new GetProgramDraftPdfQuery
            {
                CongressId = model.CongressId,
                Culture = culture,
                Cover = cover,
                Options = BuildRenderOptions(model),
                PublicBaseUrl = BuildPublicBaseUrl()
            }, cancellationToken);

            Response.Headers["Content-Disposition"] =
                $"inline; filename*=UTF-8''{Uri.EscapeDataString(response.FileName)}";
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            return File(response.Content, "application/pdf");
        }
        catch (Exception exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPost("draft-word")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DraftWord(
        [Bind(Prefix = "Export")] ProgramBookExportViewModel model,
        [FromForm(Name = "coverImageSelected")] bool coverImageSelected,
        [FromForm(Name = "Export.CoverImageFile")] IFormFile? explicitlyBoundCoverImage,
        CancellationToken cancellationToken)
    {
        if (model.CongressId == Guid.Empty)
            return BadRequest("Kongre seçimi zorunludur.");

        try
        {
            ProgramBookCoverDto cover = await ReadCoverAsync(
                model,
                coverImageSelected,
                explicitlyBoundCoverImage,
                cancellationToken);
            string? culture = RouteData.Values["culture"]?.ToString();
            ProgramDraftWordResponse response = await _mediator.Send(new GetProgramDraftWordQuery
            {
                CongressId = model.CongressId,
                Culture = culture,
                Cover = cover,
                Options = BuildRenderOptions(model),
                PublicBaseUrl = BuildPublicBaseUrl()
            }, cancellationToken);

            Response.Headers["Content-Disposition"] =
                $"attachment; filename*=UTF-8''{Uri.EscapeDataString(response.FileName)}";
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            return File(
                response.Content,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        }
        catch (Exception exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPost("reset")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reset(Guid congressId, CancellationToken cancellationToken)
    {
        string? culture = RouteData.Values["culture"]?.ToString();
        try
        {
            await _mediator.Send(new ResetCongressProgramCommand { CongressId = congressId }, cancellationToken);
            TempData["SuccessMessage"] = "Program taslağı sıfırlandı.";
        }
        catch (Exception exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }
        return RedirectToAction(nameof(Index), new { culture, congressId });
    }

    private async Task<ProgramBookCoverDto> ReadCoverAsync(
        ProgramBookExportViewModel model,
        bool coverImageSelected,
        IFormFile? explicitlyBoundCoverImage,
        CancellationToken cancellationToken)
    {
        IFormFile? file = explicitlyBoundCoverImage is { Length: > 0 }
            ? explicitlyBoundCoverImage
            : model.CoverImageFile is { Length: > 0 }
                ? model.CoverImageFile
                : null;

        // Nested IFormFile binding can stay null when Bind(Prefix = "Export") is used.
        // Resolve the file directly from the multipart collection as a safe fallback.
        if (file is null && Request.HasFormContentType)
        {
            IFormCollection form = await Request.ReadFormAsync(cancellationToken);
            file = form.Files.GetFile("Export.CoverImageFile")
                ?? form.Files.GetFile(nameof(ProgramBookExportViewModel.CoverImageFile))
                ?? form.Files.FirstOrDefault(candidate =>
                    candidate.Name.EndsWith(
                        $".{nameof(ProgramBookExportViewModel.CoverImageFile)}",
                        StringComparison.OrdinalIgnoreCase))
                ?? form.Files.FirstOrDefault();
        }

        if (coverImageSelected && file is null)
        {
            throw new InvalidOperationException(
                "Kapak dosyası seçilmiş görünüyor ancak sunucuya ulaşmadı. Sayfayı yenileyip dosyayı tekrar seçin.");
        }

        if (file is null || file.Length <= 0)
            return new ProgramBookCoverDto();

        const int maxFileSize = 8 * 1024 * 1024;
        if (file.Length > maxFileSize)
            throw new InvalidOperationException("Kapak görseli en fazla 8 MB olabilir.");

        await using MemoryStream stream = new();
        await file.CopyToAsync(stream, cancellationToken);
        byte[] bytes = stream.ToArray();
        string? contentType = DetectSupportedImageContentType(bytes);

        if (contentType is null)
        {
            throw new InvalidOperationException(
                "Kapak görseli okunamadı. PNG veya JPG formatında bir dosya yükleyin.");
        }

        _logger.LogInformation(
            "Program book cover received. FileName: {FileName}, FieldName: {FieldName}, Length: {Length}, ContentType: {ContentType}, TraceId: {TraceId}",
            file.FileName,
            file.Name,
            bytes.Length,
            contentType,
            HttpContext.TraceIdentifier);

        return new ProgramBookCoverDto
        {
            ImageBytes = bytes,
            ContentType = contentType
        };
    }

    private static string? DetectSupportedImageContentType(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 8
            && bytes[0] == 0x89
            && bytes[1] == 0x50
            && bytes[2] == 0x4E
            && bytes[3] == 0x47
            && bytes[4] == 0x0D
            && bytes[5] == 0x0A
            && bytes[6] == 0x1A
            && bytes[7] == 0x0A)
        {
            return "image/png";
        }

        if (bytes.Length >= 3
            && bytes[0] == 0xFF
            && bytes[1] == 0xD8
            && bytes[2] == 0xFF)
        {
            return "image/jpeg";
        }

        return null;
    }


    private static ProgramBookRenderOptionsDto BuildRenderOptions(ProgramBookExportViewModel model)
    {
        return new ProgramBookRenderOptionsDto
        {
            IncludeTableOfContents = model.IncludeTableOfContents,
            IncludeScheduleTimes = model.IncludeScheduleTimes,
            IncludeBoards = model.IncludeBoards
        };
    }

    private string BuildPublicBaseUrl()
    {
        return _publicUrlService.BaseUrl;
    }

    private static GenerateProgramViewModel BuildGenerateModel(ProgramManagementPageResponse page)
    {
        ProgramGenerationSourceDto? source = page.Source;
        ProgramPlanDto? plan = page.Plan;
        TimeOnly dayStartTime = new(9, 0);
        TimeOnly dayEndTime = new(19, 30);

        ProgramDayDto? firstDay = plan?.Days.OrderBy(x => x.Order).FirstOrDefault();
        if (firstDay is not null)
        {
            dayStartTime = firstDay.StartTime;
            dayEndTime = firstDay.EndTime;
        }

        ProgramSubmissionFilterDto savedFilter = plan?.SubmissionFilter ?? new ProgramSubmissionFilterDto
        {
            Preset = ProgramSubmissionScopePreset.AcceptedOnly
        };

        return new GenerateProgramViewModel
        {
            CongressId = page.SelectedCongressId ?? Guid.Empty,
            RoomIds = source?.Rooms.Select(x => x.Id).ToList() ?? new List<Guid>(),
            DayStartTime = dayStartTime,
            DayEndTime = dayEndTime,
            SessionDurationMinutes = plan?.DefaultSessionDurationMinutes ?? 120,
            PresentationDurationMinutes = plan?.DefaultPresentationDurationMinutes ?? 10,
            IncludeQuestionAnswer = plan is null || plan.DefaultQuestionAnswerDurationMinutes > 0,
            QuestionAnswerDurationMinutes = plan is not null && plan.DefaultQuestionAnswerDurationMinutes > 0
                ? plan.DefaultQuestionAnswerDurationMinutes
                : 10,
            BreakDurationMinutes = plan?.DefaultBreakDurationMinutes ?? 30,
            IncludeSessionBreaks = plan?.HasSessionBreaks ?? false,
            SessionBreakDurationMinutes = plan?.DefaultSessionBreakDurationMinutes ?? 10,
            IncludeOpening = true,
            OpeningDurationMinutes = 60,
            IncludeLunch = true,
            LunchStartTime = new TimeOnly(12, 30),
            LunchDurationMinutes = 30,
            SubmissionScopePreset = savedFilter.Preset,
            WorkflowStatusCodes = savedFilter.WorkflowStatusCodes.ToList(),
            PaymentStatusIds = savedFilter.PaymentStatusIds.ToList(),
            SubmissionTypeIds = savedFilter.SubmissionTypeIds.ToList(),
            TopicIds = savedFilter.TopicIds.ToList(),
            SubmissionSearchText = savedFilter.SearchText
        };
    }

    private Guid? GetCurrentUserId()
    {
        string? value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out Guid id) ? id : null;
    }
}

public sealed class ReorderItemsRequest
{
    public Guid CongressId { get; set; }
    public Guid MovedItemId { get; set; }
    public Guid TargetSessionId { get; set; }
    public List<Guid> OrderedItemIds { get; set; } = new();
}

public sealed class ReorderBreakRequest
{
    public Guid CongressId { get; set; }
    public Guid ProgramDayId { get; set; }
    public Guid EventRoomId { get; set; }
    public Guid BreakId { get; set; }
    public Guid? TargetSessionId { get; set; }
    public int? TargetItemIndex { get; set; }
    public List<string> OrderedBlockKeys { get; set; } = new();
}

public sealed class DeleteBreakRequest
{
    public Guid CongressId { get; set; }
    public Guid BreakId { get; set; }
}


public sealed class UpdateSessionOfficialsRequest
{
    public Guid CongressId { get; set; }
    public Guid SessionId { get; set; }
    public Guid? ChairAuthorId { get; set; }
    public Guid? ChairBoardMemberId { get; set; }
    public Guid? ViceChairAuthorId { get; set; }
    public Guid? ViceChairBoardMemberId { get; set; }
}

public sealed class MoveItemRequest
{
    public Guid CongressId { get; set; }
    public Guid ItemId { get; set; }
    public Guid TargetSessionId { get; set; }
}

public sealed class UpdateDurationRequest
{
    public Guid CongressId { get; set; }
    public Guid ItemId { get; set; }
    public int DurationMinutes { get; set; }
}

public sealed class ToggleLockRequest
{
    public Guid CongressId { get; set; }
    public Guid ItemId { get; set; }
}
