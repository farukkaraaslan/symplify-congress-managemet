using Symplify.BackOffice.Application.Common.Localization;
namespace Symplify.BackOffice.Application.Features.States.Queries.GetForUpdate;
public class GetStateForUpdateResponse
{
    public Guid Id { get; set; }
    public Guid CountryId { get; set; }
    public string? Code { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
    public List<LocalizedTranslationDto> Translations { get; set; } = new();
}
