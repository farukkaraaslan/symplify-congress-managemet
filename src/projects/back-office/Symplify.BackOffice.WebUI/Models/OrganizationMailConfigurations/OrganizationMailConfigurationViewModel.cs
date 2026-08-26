using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Symplify.BackOffice.WebUI.Models.OrganizationMailConfigurations;

public sealed class OrganizationMailConfigurationViewModel
{
    [Required]
    public Guid OrganizationId { get; set; }

    [Required, StringLength(250)]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    [Required, StringLength(250)]
    public string Username { get; set; } = string.Empty;

    [StringLength(500)]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [Required, EmailAddress, StringLength(250)]
    public string FromEmail { get; set; } = string.Empty;

    [Required, StringLength(250)]
    public string FromName { get; set; } = string.Empty;

    [EmailAddress, StringLength(250)]
    public string? ReplyToEmail { get; set; }

    [StringLength(250)]
    public string? ReplyToName { get; set; }

    public IFormFile? MailLogo { get; set; }

    public bool RemoveMailLogo { get; set; }

    public bool IsActive { get; set; } = true;
}
