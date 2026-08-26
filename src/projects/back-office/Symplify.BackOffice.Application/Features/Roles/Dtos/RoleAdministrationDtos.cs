namespace Symplify.BackOffice.Application.Features.Roles.Dtos;

public sealed class RoleListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int UserCount { get; set; }
    public int ClaimCount { get; set; }
    public DateTime CreatedDate { get; set; }
}

public sealed class RoleDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public IReadOnlyList<string> AssignedClaims { get; set; } = Array.Empty<string>();
    public IReadOnlyList<RoleClaimOptionDto> AvailableClaims { get; set; } = Array.Empty<RoleClaimOptionDto>();
}

public sealed class RoleClaimOptionDto
{
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public bool IsAssigned { get; set; }
}

public sealed class CreateRoleRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IReadOnlyCollection<string> ClaimNames { get; set; } = Array.Empty<string>();
}

public sealed class UpdateRoleRequestDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
