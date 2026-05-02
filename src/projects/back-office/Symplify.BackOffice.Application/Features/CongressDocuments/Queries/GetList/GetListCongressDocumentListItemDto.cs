namespace Symplify.BackOffice.Application.Features.CongressDocuments.Queries.GetList;
public class GetListCongressDocumentListItemDto
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public Symplify.BackOffice.Domain.Enums.CongressDocumentType? Type { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}
