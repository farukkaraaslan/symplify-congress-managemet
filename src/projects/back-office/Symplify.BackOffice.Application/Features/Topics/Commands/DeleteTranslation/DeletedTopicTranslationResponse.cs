namespace Symplify.BackOffice.Application.Features.Topics.Commands.DeleteTranslation;
public class DeletedTopicTranslationResponse
{
    public Guid Id { get; set; }
    public Guid TopicId { get; set; }
    public Guid LanguageId { get; set; }
}
