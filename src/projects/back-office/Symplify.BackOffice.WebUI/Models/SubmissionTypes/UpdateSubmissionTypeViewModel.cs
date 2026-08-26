namespace Symplify.BackOffice.WebUI.Models.SubmissionTypes;

public sealed class UpdateSubmissionTypeViewModel
{
    public Guid Id { get; set; }

    public string? Code { get; set; }

    public bool IsActive { get; set; } = true;

    public List<UpdateSubmissionTypeTranslationViewModel> Translations { get; set; } = new();
}
