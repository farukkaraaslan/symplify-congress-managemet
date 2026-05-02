namespace Symplify.BackOffice.Application.Features.Regions.Commands.DeleteTranslation;
public class DeletedRegionTranslationResponse
{
    public Guid Id { get; set; }
    public Guid RegionId { get; set; }
    public Guid LanguageId { get; set; }
}
