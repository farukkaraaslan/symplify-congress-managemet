using Symplify.BackOffice.Application.Common.Localization;
namespace Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Queries.GetForUpdate;
public class GetTransactionStatusTransitionForUpdateResponse
{
    public int Id { get; set; }
    public int FromStatusId { get; set; }
    public int ToStatusId { get; set; }
    public bool IsActive { get; set; }
    public List<LocalizedTranslationDto> Translations { get; set; } = new();
}
