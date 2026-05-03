namespace Symplify.BackOffice.Application.Services.Workflow;

public interface IWorkflowTemplateCopyService
{
    Task ApplyTemplateToCongressAsync(
        Guid congressId,
        Guid workflowTemplateId,
        bool replaceExistingTransitions,
        CancellationToken cancellationToken = default);
}
