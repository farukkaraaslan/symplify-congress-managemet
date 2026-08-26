using Symplify.BackOffice.Application.Common.Localization;
namespace Symplify.BackOffice.Application.Features.Countries.Queries.GetForUpdate;
public class GetCountryForUpdateResponse
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string? PhoneCode { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
    public List<LocalizedTranslationDto> Translations { get; set; } = new();
}
