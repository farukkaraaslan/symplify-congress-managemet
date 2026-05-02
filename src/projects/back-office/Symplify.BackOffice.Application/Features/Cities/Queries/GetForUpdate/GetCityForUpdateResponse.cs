using Symplify.BackOffice.Application.Common.Localization;
namespace Symplify.BackOffice.Application.Features.Cities.Queries.GetForUpdate;
public class GetCityForUpdateResponse
{
    public Guid Id { get; set; }
    public Guid StateId { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
    public List<LocalizedTranslationDto> Translations { get; set; } = new();
}
