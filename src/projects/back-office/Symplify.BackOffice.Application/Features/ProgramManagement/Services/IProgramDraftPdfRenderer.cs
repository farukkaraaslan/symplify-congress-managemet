using Symplify.BackOffice.Application.Features.ProgramManagement.Models;

namespace Symplify.BackOffice.Application.Features.ProgramManagement.Services;

public interface IProgramDraftPdfRenderer
{
    byte[] Render(
        string congressName,
        ProgramPlanDto plan,
        string? culture,
        ProgramBookCoverDto? cover = null,
        ProgramBookRenderOptionsDto? options = null,
        string? publicBaseUrl = null);
}
