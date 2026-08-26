namespace Symplify.BackOffice.Application.Features.DocumentTypes.Commands.Create;
public class CreatedDocumentTypeResponse
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}
