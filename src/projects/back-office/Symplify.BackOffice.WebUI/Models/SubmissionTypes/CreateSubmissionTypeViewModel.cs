namespace Symplify.BackOffice.WebUI.Models.SubmissionTypes;

public sealed class CreateSubmissionTypeViewModel
{
    public string? Code { get; set; }

    public bool IsActive { get; set; } = true;

    public List<CreateSubmissionTypeTranslationViewModel> Translations { get; set; } = new();
}
