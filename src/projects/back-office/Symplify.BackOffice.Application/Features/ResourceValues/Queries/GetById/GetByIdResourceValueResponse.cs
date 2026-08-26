namespace Symplify.BackOffice.Application.Features.ResourceValues.Queries.GetById;
public class GetByIdResourceValueResponse
{
    public Guid Id { get; set; }
    public Guid ResourceKeyId { get; set; }
    public Guid LanguageId { get; set; }
    public string Value { get; set; } = string.Empty;
}
