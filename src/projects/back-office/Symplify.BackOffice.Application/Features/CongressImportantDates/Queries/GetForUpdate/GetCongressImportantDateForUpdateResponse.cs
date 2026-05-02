using Symplify.BackOffice.Application.Common.Localization;
namespace Symplify.BackOffice.Application.Features.CongressImportantDates.Queries.GetForUpdate;
public class GetCongressImportantDateForUpdateResponse
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public DateTime Date { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public List<LocalizedTranslationDto> Translations { get; set; } = new();
}
