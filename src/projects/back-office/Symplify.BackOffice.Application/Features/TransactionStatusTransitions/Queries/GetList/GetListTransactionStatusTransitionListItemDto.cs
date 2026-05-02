namespace Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Queries.GetList;
public class GetListTransactionStatusTransitionListItemDto
{
    public int Id { get; set; }
    public int FromStatusId { get; set; }
    public int ToStatusId { get; set; }
    public bool IsActive { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid DisplayLanguageId { get; set; }
    public bool IsFallback { get; set; }
}
