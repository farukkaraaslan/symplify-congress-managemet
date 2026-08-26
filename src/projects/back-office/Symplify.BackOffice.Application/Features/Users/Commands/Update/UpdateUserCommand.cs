using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.Users.Constants;
using Symplify.BackOffice.Application.Features.Users.Dtos;
using Symplify.BackOffice.Application.Services.UserAdministration;

namespace Symplify.BackOffice.Application.Features.Users.Commands.Update;

public sealed class UpdateUserCommand : IRequest<UserDetailDto>, ISecuredRequest
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
    public bool LockoutEnabled { get; set; } = true;
    public Guid? OrganizationAccessId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? DefaultCongressId { get; set; }
    public bool OrganizationAccessIsActive { get; set; } = true;

    public string[] Roles => new[]
    {
        "SuperAdmin",
        "OrganizationAdmin",
        UsersOperationClaims.Admin,
        UsersOperationClaims.Update
    };

    public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDetailDto>
    {
        private readonly IUserAdministrationService _service;

        public UpdateUserCommandHandler(IUserAdministrationService service)
        {
            _service = service;
        }

        public Task<UserDetailDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            return _service.UpdateAsync(new UpdateUserRequestDto
            {
                Id = request.Id,
                Email = request.Email,
                Name = request.Name,
                Surname = request.Surname,
                Institution = request.Institution,
                TitleId = request.TitleId,
                CountryId = request.CountryId,
                StateId = request.StateId,
                Orcid = request.Orcid,
                PhoneNumber = request.PhoneNumber,
                EmailConfirmed = request.EmailConfirmed,
                LockoutEnabled = request.LockoutEnabled,
                OrganizationAccessId = request.OrganizationAccessId,
                OrganizationId = request.OrganizationId,
                DefaultCongressId = request.DefaultCongressId,
                OrganizationAccessIsActive = request.OrganizationAccessIsActive
            }, cancellationToken);
        }
    }
}
