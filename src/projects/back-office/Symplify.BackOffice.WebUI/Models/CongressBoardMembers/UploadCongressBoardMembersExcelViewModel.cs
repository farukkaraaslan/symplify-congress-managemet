using Microsoft.AspNetCore.Http;

namespace Symplify.BackOffice.WebUI.Models.CongressBoardMembers;

public sealed class UploadCongressBoardMembersExcelViewModel
{
    public Guid CongressId { get; set; }

    public IFormFile? File { get; set; }
}
