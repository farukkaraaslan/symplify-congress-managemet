using Microsoft.AspNetCore.Http;

namespace Symplify.BackOffice.WebUI.Models.CongressBoardMembers;

public sealed class UpdateCongressBoardMemberViewModel
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public Guid? CongressBoardId { get; set; }
    public string? BoardName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AcademicTitle { get; set; }
    public string? Institution { get; set; }
    public string? ImagePath { get; set; }
    public string? ImagePreviewUrl { get; set; }
    public IFormFile? ImageFile { get; set; }
    public bool IsAcceptanceLetterSigner { get; set; }
    public string? SignaturePath { get; set; }
    public string? SignaturePreviewUrl { get; set; }
    public IFormFile? SignatureFile { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
    public List<CongressBoardSelectItemViewModel> BoardOptions { get; set; } = new();
    public List<string> AcademicTitleOptions { get; set; } = new();
    public List<CongressBoardMemberTranslationViewModel> Translations { get; set; } = new();
}
