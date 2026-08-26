namespace Symplify.BackOffice.Application.Features.WorkflowTemplates.Queries.GetList;

public class GetListWorkflowTemplateListItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int? InitialTransactionStatusId { get; set; }
    public string? InitialTransactionStatusName { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid DisplayLanguageId { get; set; }
    public bool IsFallback { get; set; }
}
