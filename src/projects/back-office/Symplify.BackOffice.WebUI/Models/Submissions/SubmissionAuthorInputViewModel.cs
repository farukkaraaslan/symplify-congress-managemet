using System.ComponentModel.DataAnnotations;

namespace Symplify.BackOffice.WebUI.Models.Submissions;

public sealed class SubmissionAuthorInputViewModel
{
    public Guid? Id { get; set; }

    [Required]
    public Guid? TitleId { get; set; }

    public string? TitleName { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string? Institution { get; set; }

    public string? Orcid { get; set; }

    public bool IsCorrespondingAuthor { get; set; }
}
