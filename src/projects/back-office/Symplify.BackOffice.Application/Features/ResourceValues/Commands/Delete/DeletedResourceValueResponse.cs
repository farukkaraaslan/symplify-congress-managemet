namespace Symplify.BackOffice.Application.Features.ResourceValues.Commands.Delete;
public class DeletedResourceValueResponse
{
    public Guid Id { get; set; }
    public Guid ResourceKeyId { get; set; }
    public Guid LanguageId { get; set; }
    public string Value { get; set; } = string.Empty;
}
