using Symplify.BackOffice.Application.Common.Localization;
namespace Symplify.BackOffice.Application.Features.SubmissionTypes.Queries.GetForUpdate;
public class GetSubmissionTypeForUpdateResponse
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public List<LocalizedTranslationDto> Translations { get; set; } = new();
}
