using Symplify.BackOffice.Application.Common.Localization;
namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Queries.GetForUpdate;
public class GetCongressBoardMemberForUpdateResponse
{
    public Guid Id { get; set; }
    public Guid CongressBoardId { get; set; }
    public string? ImagePath { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public List<LocalizedTranslationDto> Translations { get; set; } = new();
}
