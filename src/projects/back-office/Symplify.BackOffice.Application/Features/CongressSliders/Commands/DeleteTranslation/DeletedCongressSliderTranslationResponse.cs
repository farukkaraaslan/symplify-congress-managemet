namespace Symplify.BackOffice.Application.Features.CongressSliders.Commands.DeleteTranslation;
public class DeletedCongressSliderTranslationResponse
{
    public Guid Id { get; set; }
    public Guid CongressSliderId { get; set; }
    public Guid LanguageId { get; set; }
}
