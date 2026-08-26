using Core.Application.Responses;
using Symplify.BackOffice.Application.Features.Roles.Dtos;

namespace Symplify.BackOffice.Application.Services.RoleAdministration;

public interface IRoleAdministrationService
{
    Task<GetListResponse<RoleListItemDto>> GetListAsync(
        int page,
        int pageSize,
        string? searchText,
        CancellationToken cancellationToken = default);

    Task<RoleDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RoleDetailDto> CreateAsync(CreateRoleRequestDto request, CancellationToken cancellationToken = default);

    Task<RoleDetailDto> UpdateAsync(UpdateRoleRequestDto request, CancellationToken cancellationToken = default);

    Task UpdateClaimsAsync(Guid roleId, IReadOnlyCollection<string> claimNames, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid roleId, CancellationToken cancellationToken = default);
}
