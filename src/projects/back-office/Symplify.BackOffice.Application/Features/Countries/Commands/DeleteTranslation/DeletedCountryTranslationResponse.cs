namespace Symplify.BackOffice.Application.Features.Countries.Commands.DeleteTranslation;
public class DeletedCountryTranslationResponse
{
    public Guid Id { get; set; }
    public Guid CountryId { get; set; }
    public Guid LanguageId { get; set; }
}
