using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands.UpdateAcceptanceSignature;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Queries.GetForUpdate;
using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.WebUI.Models.CongressBoardMembers;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture=tr-TR}/congress-board-member-signatures/[action]")]
public sealed class CongressBoardMemberSignaturesController : Controller
{
    private const long MaxSignatureSizeInBytes = 2 * 1024 * 1024;

    private static readonly HashSet<string> AllowedSignatureExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png"
    };

    private readonly IMediator _mediator;
    private readonly IBackOfficeViewLocalizer _localizer;

    public CongressBoardMemberSignaturesController(IMediator mediator, IBackOfficeViewLocalizer localizer)
    {
        _mediator = mediator;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> EditModal(Guid id, Guid congressId, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty || congressId == Guid.Empty)
            return BadRequest(T("Common.InvalidRequest", "Geçersiz istek."));

        GetCongressBoardMemberForUpdateResponse response = await _mediator.Send(
            new GetCongressBoardMemberForUpdateQuery
            {
                Id = id,
                CongressId = congressId
            },
            cancellationToken);

        UpdateCongressBoardMemberViewModel model = new()
        {
            Id = response.Id,
            CongressId = response.CongressId,
            FullName = response.FullName,
            AcademicTitle = response.AcademicTitle,
            Institution = response.Institution,
            ImagePath = response.ImagePath,
            ImagePreviewUrl = BuildMediaUrl(
                "Photo",
                response.Id,
                response.CongressId,
                !string.IsNullOrWhiteSpace(response.ImageObjectName) ||
                !string.IsNullOrWhiteSpace(response.ImagePath)),
            IsAcceptanceLetterSigner = response.IsAcceptanceLetterSigner,
            SignaturePath = response.SignaturePath,
            SignaturePreviewUrl = BuildMediaUrl(
                "Signature",
                response.Id,
                response.CongressId,
                !string.IsNullOrWhiteSpace(response.SignatureObjectName) ||
                !string.IsNullOrWhiteSpace(response.SignaturePath))
        };

        return PartialView("~/Views/CongressBoardMembers/_UpdateAcceptanceSignatureModal.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        [FromForm] Guid id,
        [FromForm] Guid congressId,
        [FromForm] bool isAcceptanceLetterSigner,
        [FromForm] IFormFile? signatureFile,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty || congressId == Guid.Empty)
            return BadRequest(new { success = false, message = T("Common.InvalidRequest", "Geçersiz istek.") });

        if (signatureFile is not null && signatureFile.Length > 0)
        {
            string extension = Path.GetExtension(signatureFile.FileName);

            if (!AllowedSignatureExtensions.Contains(extension))
                return BadRequest(new { success = false, message = T("BackOffice.CongressBoardMembers.Validation.SignatureExtensionInvalid", "Sadece PNG veya JPG imza görseli yükleyebilirsiniz.") });

            if (signatureFile.Length > MaxSignatureSizeInBytes)
                return BadRequest(new { success = false, message = T("BackOffice.CongressBoardMembers.Validation.SignatureSizeInvalid", "İmza görseli en fazla 2 MB olabilir.") });
        }

        Stream? signatureStream = null;

        try
        {
            CongressBoardMemberSignatureInputDto? signature = null;

            if (signatureFile is not null && signatureFile.Length > 0)
            {
                signatureStream = signatureFile.OpenReadStream();
                signature = new CongressBoardMemberSignatureInputDto
                {
                    OriginalFileName = signatureFile.FileName,
                    ContentType = signatureFile.ContentType,
                    Length = signatureFile.Length,
                    Content = signatureStream
                };
            }

            await _mediator.Send(
                new UpdateCongressBoardMemberAcceptanceSignatureCommand
                {
                    Id = id,
                    CongressId = congressId,
                    IsAcceptanceLetterSigner = isAcceptanceLetterSigner,
                    Signature = signature
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                message = T("BackOffice.CongressBoardMembers.Messages.SignatureUpdated", "Kabul mektubu imza ayarları güncellendi.")
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new
            {
                success = false,
                message = T(exception.Message, exception.Message)
            });
        }
        finally
        {
            if (signatureStream is not null)
                await signatureStream.DisposeAsync();
        }
    }


    private string? BuildMediaUrl(
        string action,
        Guid id,
        Guid congressId,
        bool hasMedia)
    {
        if (!hasMedia || id == Guid.Empty || congressId == Guid.Empty)
            return null;

        string culture = RouteData.Values["culture"]?.ToString() ?? "tr-TR";

        return Url.Action(
            action,
            "CongressBoardMemberMedia",
            new
            {
                culture,
                congressId,
                id
            });
    }


    private string T(string key, string fallback)
    {
        string value = _localizer.GetStringValue(key);
        return string.IsNullOrWhiteSpace(value) || string.Equals(value, key, StringComparison.OrdinalIgnoreCase)
            ? fallback
            : value;
    }
}
