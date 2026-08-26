namespace Symplify.BackOffice.WebUI.Models.CongressSubmissionTypes;

public sealed class SaveCongressSubmissionTypeSelectionsViewModel
{
    public Guid CongressId { get; set; }
    public List<Guid> SelectedSubmissionTypeIds { get; set; } = new();
}
