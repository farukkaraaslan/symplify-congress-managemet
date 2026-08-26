using Microsoft.AspNetCore.Mvc.Rendering;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.WebUI.Models.BulkEmails;

public sealed class BulkEmailComposeViewModel
{
    public Guid CongressId { get; set; }

    public BulkEmailAudienceType AudienceType { get; set; } = BulkEmailAudienceType.AllRegistered;

    public string Culture { get; set; } = "tr-TR";

    public string Subject { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string BodyText { get; set; } = string.Empty;

    public string ExcludedRecipientEmailsJson { get; set; } = "[]";

    public string AdditionalRecipientsJson { get; set; } = "[]";

    public IReadOnlyList<SelectListItem> CongressOptions { get; set; } = Array.Empty<SelectListItem>();
}
