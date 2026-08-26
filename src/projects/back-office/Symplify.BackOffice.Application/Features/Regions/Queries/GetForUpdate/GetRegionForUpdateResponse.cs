using Symplify.BackOffice.Application.Common.Localization;
namespace Symplify.BackOffice.Application.Features.Regions.Queries.GetForUpdate;
public class GetRegionForUpdateResponse
{
    public Guid Id { get; set; }
    public Guid? CountryId { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
    public List<LocalizedTranslationDto> Translations { get; set; } = new();
}
