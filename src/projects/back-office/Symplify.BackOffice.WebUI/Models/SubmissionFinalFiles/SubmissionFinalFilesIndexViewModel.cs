using Microsoft.AspNetCore.Mvc.Rendering;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.WebUI.Models.SubmissionFinalFiles;

public sealed class SubmissionFinalFilesIndexViewModel
{
    public SubmissionFileKind FileKind { get; set; }

    public string PageTitleKey { get; set; } = string.Empty;

    public string ListTitleKey { get; set; } = string.Empty;

    public string ListDescriptionKey { get; set; } = string.Empty;

    public string SourceUrl { get; set; } = string.Empty;

    public string ReviewUrl { get; set; } = string.Empty;

    public string BulkReviewUrl { get; set; } = string.Empty;

    public string BulkDownloadUrl { get; set; } = string.Empty;

    public string GenerateFullTextBookUrl { get; set; } = string.Empty;

    public string BulkDeleteUrl { get; set; } = string.Empty;

    public string DeleteUrl { get; set; } = string.Empty;

    public string ToggleProgramBookUrl { get; set; } = string.Empty;

    public string PublicLinksUrl { get; set; } = string.Empty;

    public bool IsVideoPage { get; set; }

    public bool ArchiveMode { get; set; }

    public string ArchiveToggleUrl { get; set; } = string.Empty;

    public IReadOnlyList<SelectListItem> CongressOptions { get; set; } = Array.Empty<SelectListItem>();
}
