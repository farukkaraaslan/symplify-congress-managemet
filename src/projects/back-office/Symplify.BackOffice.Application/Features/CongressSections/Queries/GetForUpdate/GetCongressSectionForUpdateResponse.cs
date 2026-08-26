using Symplify.BackOffice.Application.Common.Localization;
namespace Symplify.BackOffice.Application.Features.CongressSections.Queries.GetForUpdate;
public class GetCongressSectionForUpdateResponse
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public string BindingKey { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public List<LocalizedTranslationDto> Translations { get; set; } = new();
}
