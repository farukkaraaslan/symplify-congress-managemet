namespace Symplify.BackOffice.Application.Features.CongressImportantDates.Commands.Update;

public class UpdatedCongressImportantDateResponse
{
    public Guid Id { get; set; }

    public Guid CongressId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int Order { get; set; }

    public bool IsActive { get; set; }
}
