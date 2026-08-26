using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.Users.Constants;
using Symplify.BackOffice.Application.Features.Users.Dtos;
using Symplify.BackOffice.Application.Services.UserAdministration;

namespace Symplify.BackOffice.Application.Features.Users.Commands.Create;

public sealed class CreateUserCommand : IRequest<CreatedUserDto>, ISecuredRequest
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string? Institution { get; set; }
    public string? Orcid { get; set; }
    public string? PhoneNumber { get; set; }
    public bool EmailConfirmed { get; set; } = true;
    public bool GeneratePassword { get; set; } = true;
    public string? Password { get; set; }
    public List<string> RoleNames { get; set; } = new();

    public string[] Roles => new[]
    {
        "SuperAdmin",
        "OrganizationAdmin",
        UsersOperationClaims.Admin,
        UsersOperationClaims.Add
    };

    public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, CreatedUserDto>
    {
        private readonly IUserAdministrationService _service;

        public CreateUserCommandHandler(IUserAdministrationService service)
        {
            _service = service;
        }

        public Task<CreatedUserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            return _service.CreateAsync(new CreateUserRequestDto
            {
                Email = request.Email,
                Name = request.Name,
                Surname = request.Surname,
                Institution = request.Institution,
                Orcid = request.Orcid,
                PhoneNumber = request.PhoneNumber,
                EmailConfirmed = request.EmailConfirmed,
                GeneratePassword = request.GeneratePassword,
                Password = request.Password,
                RoleNames = request.RoleNames
            }, cancellationToken);
        }
    }
}
