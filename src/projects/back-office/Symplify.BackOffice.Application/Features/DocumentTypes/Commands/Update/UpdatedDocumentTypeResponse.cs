namespace Symplify.BackOffice.Application.Features.DocumentTypes.Commands.Update;
public class UpdatedDocumentTypeResponse
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}
