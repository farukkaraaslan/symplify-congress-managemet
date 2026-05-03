namespace Symplify.BackOffice.Application.Features.WorkflowTemplates.Commands.Update;

public class UpdatedWorkflowTemplateResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int? InitialTransactionStatusId { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}
