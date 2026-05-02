namespace Symplify.BackOffice.Application.Features.CongressImportantDates.Commands.Create;
public class CreatedCongressImportantDateResponse
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public DateTime Date { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}
