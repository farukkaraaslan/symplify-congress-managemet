namespace Symplify.BackOffice.Application.Features.Titles.Commands.DeleteTranslation;
public class DeletedTitleTranslationResponse
{
    public Guid Id { get; set; }
    public Guid TitleId { get; set; }
    public Guid LanguageId { get; set; }
}
