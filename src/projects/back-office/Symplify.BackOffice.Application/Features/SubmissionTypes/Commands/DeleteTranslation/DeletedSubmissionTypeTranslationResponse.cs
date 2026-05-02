namespace Symplify.BackOffice.Application.Features.SubmissionTypes.Commands.DeleteTranslation;
public class DeletedSubmissionTypeTranslationResponse
{
    public Guid Id { get; set; }
    public Guid SubmissionTypeId { get; set; }
    public Guid LanguageId { get; set; }
}
