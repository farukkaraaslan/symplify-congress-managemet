using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Symplify.BackOffice.WebUI.Models.ExhibitionApplications;

public sealed class CreateExhibitionApplicationViewModel
{
    [Required]
    public Guid CongressId { get; set; }

    public string CongressName { get; set; } = string.Empty;

    [Required]
    public Guid? SubmissionTypeId { get; set; }

    public string SubmissionTypeName { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string WorkName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Dimensions { get; set; }

    [Required]
    [MaxLength(250)]
    public string Technique { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Address { get; set; } = string.Empty;

    [Required]
    public IFormFile? ExhibitionFile { get; set; }
}
