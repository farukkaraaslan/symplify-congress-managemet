namespace Symplify.BackOffice.Application.Features.TransactionStatuses.Queries.GetList;

public class GetListTransactionStatusListItemDto
{
    public int Id { get; set; }

    public int TransactionStatusPhaseId { get; set; }

    public string TransactionStatusPhaseCode { get; set; } = string.Empty;

    public string TransactionStatusPhaseName { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public int Order { get; set; }

    public bool IsEditable { get; set; }

    public bool IsFinal { get; set; }

    public bool IsActive { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid DisplayLanguageId { get; set; }

    public bool IsFallback { get; set; }
}
