namespace Symplify.BackOffice.WebUI.Models.SubmissionTypes;

public sealed class SubmissionTypesIndexViewModel
{
    public CreateSubmissionTypeViewModel CreateSubmissionType { get; set; } = new();

    public UpdateSubmissionTypeViewModel UpdateSubmissionType { get; set; } = new();
}
