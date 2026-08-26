using Symplify.BackOffice.Application.Common.Localization;
namespace Symplify.BackOffice.Application.Features.CongressBoards.Queries.GetForUpdate;
public class GetCongressBoardForUpdateResponse
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public List<LocalizedTranslationDto> Translations { get; set; } = new();
}
