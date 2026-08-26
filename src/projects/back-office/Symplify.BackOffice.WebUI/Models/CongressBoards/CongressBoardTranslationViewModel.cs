using System.ComponentModel.DataAnnotations;

namespace Symplify.BackOffice.WebUI.Models.CongressBoards;

public sealed class CongressBoardTranslationViewModel
{
    public Guid LanguageId { get; set; }

    public string Culture { get; set; } = string.Empty;

    public string LanguageName { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public bool Exists { get; set; }

    [MaxLength(250)]
    public string? Name { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }
}
