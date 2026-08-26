namespace Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Queries.GetList;

public class GetListTransactionStatusTransitionListItemDto
{
    public int Id { get; set; }

    public int FromStatusId { get; set; }

    public string FromStatusCode { get; set; } = string.Empty;

    public string FromStatusName { get; set; } = string.Empty;

    public int ToStatusId { get; set; }

    public string ToStatusCode { get; set; } = string.Empty;

    public string ToStatusName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid DisplayLanguageId { get; set; }

    public bool IsFallback { get; set; }
}
