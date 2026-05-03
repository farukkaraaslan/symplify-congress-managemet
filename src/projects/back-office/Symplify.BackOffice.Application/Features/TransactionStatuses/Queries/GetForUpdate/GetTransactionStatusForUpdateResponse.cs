using Symplify.BackOffice.Application.Common.Localization;

namespace Symplify.BackOffice.Application.Features.TransactionStatuses.Queries.GetForUpdate;

public class GetTransactionStatusForUpdateResponse
{
    public int Id { get; set; }

    public int TransactionStatusPhaseId { get; set; }

    public string Code { get; set; } = string.Empty;

    public int Order { get; set; }

    public bool IsEditable { get; set; }

    public bool IsFinal { get; set; }

    public bool IsActive { get; set; }

    public List<LocalizedTranslationDto> Translations { get; set; } = new();
}
