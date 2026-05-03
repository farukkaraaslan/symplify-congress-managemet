namespace Symplify.BackOffice.Application.Features.TransactionStatusPhases.Queries.GetById;

public class GetByIdTransactionStatusPhaseResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public int Order { get; set; }

    public bool IsActive { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid DisplayLanguageId { get; set; }

    public bool IsFallback { get; set; }
}
