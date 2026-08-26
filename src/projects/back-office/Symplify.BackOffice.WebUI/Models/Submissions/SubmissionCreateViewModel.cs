using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.WebUI.Models.Submissions;

public sealed class SubmissionCreateViewModel
{
    [Required]
    public Guid CongressId { get; set; }

    public string CongressName { get; set; } = string.Empty;

    [Required]
    public Guid? SubmissionTypeId { get; set; }

    public bool IsSubmissionTypeLocked { get; set; }

    public string? SelectedSubmissionTypeName { get; set; }

    public SubmissionFormProfile SelectedSubmissionTypeFormProfile { get; set; } = SubmissionFormProfile.AcademicAbstract;

    [Required]
    public Guid? TopicId { get; set; }

    public Guid? LanguageId { get; set; }

    public string? Orcid { get; set; }

    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? TitleEn { get; set; }

    [Required]
    public string Abstract { get; set; } = string.Empty;

    public string? AbstractEn { get; set; }

    [Required]
    [MaxLength(500)]
    public string Keywords { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? KeywordsEn { get; set; }

    public string SubmitAction { get; set; } = "submit";

    public List<SubmissionAuthorInputViewModel> Authors { get; set; } = new();

    public IReadOnlyList<SubmissionCreateSelectItemViewModel> TitleOptions { get; set; } = Array.Empty<SubmissionCreateSelectItemViewModel>();

    public IFormFile? FullTextFile { get; set; }

    public IFormFile? PresentationFile { get; set; }

    public IReadOnlyList<SubmissionCreateSelectItemViewModel> Congresses { get; set; } = Array.Empty<SubmissionCreateSelectItemViewModel>();

    public IReadOnlyList<SubmissionCreateSelectItemViewModel> SubmissionTypes { get; set; } = Array.Empty<SubmissionCreateSelectItemViewModel>();

    public IReadOnlyList<SubmissionCreateSelectItemViewModel> Topics { get; set; } = Array.Empty<SubmissionCreateSelectItemViewModel>();

    public IReadOnlyList<SubmissionCreateSelectItemViewModel> Languages { get; set; } = Array.Empty<SubmissionCreateSelectItemViewModel>();
}
