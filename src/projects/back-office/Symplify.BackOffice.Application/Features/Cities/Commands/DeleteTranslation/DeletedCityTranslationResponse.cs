namespace Symplify.BackOffice.Application.Features.Cities.Commands.DeleteTranslation;
public class DeletedCityTranslationResponse
{
    public Guid Id { get; set; }
    public Guid CityId { get; set; }
    public Guid LanguageId { get; set; }
}
