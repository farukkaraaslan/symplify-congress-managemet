namespace Symplify.BackOffice.Application.Features.CongressSliders.Commands.Delete;
public class DeletedCongressSliderResponse
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; }
}
