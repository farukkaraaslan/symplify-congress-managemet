using Symplify.BackOffice.Application.Common.Localization;

namespace Symplify.BackOffice.Application.Features.WorkflowTemplates.Queries.GetForUpdate;

public class GetWorkflowTemplateForUpdateResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int? InitialTransactionStatusId { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public List<LocalizedTranslationDto> Translations { get; set; } = new();
}
