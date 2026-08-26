namespace Symplify.BackOffice.Application.Features.ResourceKeys.Commands.Delete;
public class DeletedResourceKeyResponse
{
    public Guid Id { get; set; }
    public string KeyName { get; set; } = string.Empty;
    public string? AreaName { get; set; }
    public string? Description { get; set; }
}
