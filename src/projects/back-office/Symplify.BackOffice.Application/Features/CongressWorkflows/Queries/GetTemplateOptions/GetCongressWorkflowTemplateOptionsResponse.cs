namespace Symplify.BackOffice.Application.Features.CongressWorkflows.Queries.GetTemplateOptions;

public sealed class GetCongressWorkflowTemplateOptionsResponse
{
    public List<CongressWorkflowTemplateOptionDto> Items { get; set; } = new();
}

public sealed class CongressWorkflowTemplateOptionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? InitialTransactionStatusId { get; set; }
    public string? InitialTransactionStatusName { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public Guid DisplayLanguageId { get; set; }
    public bool IsFallback { get; set; }
}
