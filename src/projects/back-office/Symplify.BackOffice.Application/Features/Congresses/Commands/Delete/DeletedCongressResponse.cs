namespace Symplify.BackOffice.Application.Features.Congresses.Commands.Delete;
public class DeletedCongressResponse
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Symplify.BackOffice.Domain.Enums.CongressStatus Status { get; set; }
    public bool IsActive { get; set; }
}
