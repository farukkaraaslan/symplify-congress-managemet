namespace Symplify.BackOffice.Application.Features.Titles.Commands.Create;
public class CreatedTitleResponse
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}
