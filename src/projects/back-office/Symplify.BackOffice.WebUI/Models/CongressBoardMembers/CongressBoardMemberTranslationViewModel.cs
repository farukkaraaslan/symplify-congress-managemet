namespace Symplify.BackOffice.WebUI.Models.CongressBoardMembers;

public sealed class CongressBoardMemberTranslationViewModel
{
    public Guid LanguageId { get; set; }

    public string Culture { get; set; } = string.Empty;

    public string LanguageName { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public bool Exists { get; set; }

    public string? Biography { get; set; }
}
