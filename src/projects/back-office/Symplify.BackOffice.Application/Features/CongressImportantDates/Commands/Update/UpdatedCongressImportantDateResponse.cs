namespace Symplify.BackOffice.Application.Features.CongressImportantDates.Commands.Update;
public class UpdatedCongressImportantDateResponse
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public DateTime Date { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}
