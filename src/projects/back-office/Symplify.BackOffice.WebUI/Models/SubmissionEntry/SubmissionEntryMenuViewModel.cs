namespace Symplify.BackOffice.WebUI.Models.SubmissionEntry;

public sealed class SubmissionEntryMenuViewModel
{
    public string DisplayMode { get; set; } = "Sidebar";

    public IReadOnlyList<SubmissionEntryMenuItemViewModel> Items { get; set; } = Array.Empty<SubmissionEntryMenuItemViewModel>();
}
