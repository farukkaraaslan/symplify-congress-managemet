namespace Symplify.BackOffice.Application.Features.CongressSections.Commands.Create;
public class CreatedCongressSectionResponse
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public string BindingKey { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; }
}
