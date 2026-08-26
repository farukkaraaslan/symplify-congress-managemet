namespace Symplify.BackOffice.Application.Features.Users.Dtos;

public sealed class UserListItemDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? TitleShortName { get; set; }
    public string? Institution { get; set; }
    public string? Orcid { get; set; }
    public string? CountryName { get; set; }
    public string? StateName { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string OrganizationShortName { get; set; } = string.Empty;
    public string DefaultCongressName { get; set; } = string.Empty;
    public string RolesText { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public bool IsBlacklisted { get; set; }
    public bool IsLockedOut { get; set; }
    public bool OrganizationAccessIsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
}

public sealed class UserDetailDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Institution { get; set; }
    public Guid? TitleId { get; set; }
    public string? TitleName { get; set; }
    public string? TitleShortName { get; set; }
    public Guid? CountryId { get; set; }
    public string? CountryName { get; set; }
    public Guid? StateId { get; set; }
    public string? StateName { get; set; }
    public string? Orcid { get; set; }
    public string? PhoneNumber { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool IsBlacklisted { get; set; }
    public bool LockoutEnabled { get; set; }
    public bool IsLockedOut { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public int AccessFailedCount { get; set; }
    public Guid? OrganizationAccessId { get; set; }
    public Guid? OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
    public string? OrganizationShortName { get; set; }
    public Guid? DefaultCongressId { get; set; }
    public string? DefaultCongressName { get; set; }
    public bool OrganizationAccessIsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public IReadOnlyList<string> AssignedRoles { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> AssignedClaims { get; set; } = Array.Empty<string>();
    public IReadOnlyList<RoleOptionDto> AvailableRoles { get; set; } = Array.Empty<RoleOptionDto>();
    public IReadOnlyList<ClaimOptionDto> AvailableClaims { get; set; } = Array.Empty<ClaimOptionDto>();
}

public sealed class RoleOptionDto
{
    public string Name { get; set; } = string.Empty;
    public bool IsAssigned { get; set; }
}

public sealed class ClaimOptionDto
{
    public string Name { get; set; } = string.Empty;
    public bool IsAssigned { get; set; }
}

public sealed class CreateUserRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string? Institution { get; set; }
    public Guid? TitleId { get; set; }
    public Guid? CountryId { get; set; }
    public Guid? StateId { get; set; }
    public string? Orcid { get; set; }
    public string? PhoneNumber { get; set; }
    public bool EmailConfirmed { get; set; } = true;
    public string? Password { get; set; }
    public bool GeneratePassword { get; set; }
    public IReadOnlyCollection<string> RoleNames { get; set; } = Array.Empty<string>();
}

public sealed class UpdateUserRequestDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string? Institution { get; set; }
    public Guid? TitleId { get; set; }
    public Guid? CountryId { get; set; }
    public Guid? StateId { get; set; }
    public string? Orcid { get; set; }
    public string? PhoneNumber { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool LockoutEnabled { get; set; }
    public Guid? OrganizationAccessId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? DefaultCongressId { get; set; }
    public bool OrganizationAccessIsActive { get; set; } = true;
}

public sealed class CreatedUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string GeneratedPassword { get; set; } = string.Empty;
}

public sealed class ResetUserPasswordDto
{
    public Guid UserId { get; set; }
    public string GeneratedPassword { get; set; } = string.Empty;
    public int RemainingAttemptsInWindow { get; set; }
}
