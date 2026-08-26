using System.ComponentModel.DataAnnotations;

namespace Symplify.BackOffice.WebUI.Models.OrganizationMailConfigurations;

public sealed class SendOrganizationMailTestViewModel
{
    [Required]
    public Guid OrganizationId { get; set; }

    [Required, EmailAddress, StringLength(250)]
    public string ToEmail { get; set; } = string.Empty;

    [StringLength(250)]
    public string? ToName { get; set; }
}
