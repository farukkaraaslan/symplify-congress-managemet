using Symplify.BackOffice.Application.Common.Localization;

namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Queries.GetForUpdate;

public sealed class GetCongressBoardMemberForUpdateResponse
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public Guid CongressBoardId { get; set; }
    public string? BoardName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AcademicTitle { get; set; }
    public string? Institution { get; set; }
    public string? ImagePath { get; set; }
    public string? ImagePreviewUrl { get; set; }
    public string? ImageBucketName { get; set; }
    public string? ImageObjectName { get; set; }
    public string? ImageFileName { get; set; }
    public string? ImageContentType { get; set; }
    public long? ImageFileSize { get; set; }
    public bool IsAcceptanceLetterSigner { get; set; }
    public string? SignaturePath { get; set; }
    public string? SignaturePreviewUrl { get; set; }
    public string? SignatureBucketName { get; set; }
    public string? SignatureObjectName { get; set; }
    public string? SignatureFileName { get; set; }
    public string? SignatureContentType { get; set; }
    public long? SignatureFileSize { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public List<LocalizedTranslationDto> Translations { get; set; } = new();
}
