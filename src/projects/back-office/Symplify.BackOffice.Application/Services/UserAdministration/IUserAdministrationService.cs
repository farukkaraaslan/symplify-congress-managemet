using Core.Application.Responses;
using Symplify.BackOffice.Application.Features.Users.Dtos;

namespace Symplify.BackOffice.Application.Services.UserAdministration;

public interface IUserAdministrationService
{
    Task<GetListResponse<UserListItemDto>> GetListAsync(
        int page,
        int pageSize,
        string? searchText,
        bool? isBlacklisted,
        Guid? organizationId,
        bool? emailConfirmed,
        Guid? countryId,
        Guid? stateId,
        Guid? congressId,
        string? roleName,
        string? accountStatus,
        string? culture,
        string? sortColumn = null,
        string? sortDirection = null,
        CancellationToken cancellationToken = default);

    Task<UserDetailDto> GetByIdAsync(
        Guid id,
        string? culture = null,
        CancellationToken cancellationToken = default);

    Task<CreatedUserDto> CreateAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default);

    Task<UserDetailDto> UpdateAsync(UpdateUserRequestDto request, CancellationToken cancellationToken = default);

    Task<ResetUserPasswordDto> ResetPasswordAsync(Guid userId, CancellationToken cancellationToken = default);

    Task UpdateRolesAsync(Guid userId, IReadOnlyCollection<string> roleNames, CancellationToken cancellationToken = default);

    Task UpdateClaimsAsync(Guid userId, IReadOnlyCollection<string> claimNames, CancellationToken cancellationToken = default);

    Task SetBlacklistAsync(Guid userId, bool isBlacklisted, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(Guid userId, CancellationToken cancellationToken = default);
}
