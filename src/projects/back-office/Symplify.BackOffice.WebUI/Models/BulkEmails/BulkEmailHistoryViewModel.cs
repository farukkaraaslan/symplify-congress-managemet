using Microsoft.AspNetCore.Mvc.Rendering;

namespace Symplify.BackOffice.WebUI.Models.BulkEmails;

public sealed class BulkEmailHistoryViewModel
{
    public Guid CongressId { get; set; }

    public string Culture { get; set; } = "tr-TR";

    public IReadOnlyList<SelectListItem> CongressOptions { get; set; } = Array.Empty<SelectListItem>();
}
