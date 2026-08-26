using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.Users.Constants;
using Symplify.BackOffice.Application.Features.Users.Dtos;
using Symplify.BackOffice.Application.Services.UserAdministration;

namespace Symplify.BackOffice.Application.Features.Users.Queries.GetById;

public sealed class GetByIdUserQuery : IRequest<UserDetailDto>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string? Culture { get; set; }

    public string[] Roles => new[]
    {
        "SuperAdmin",
        "OrganizationAdmin",
        UsersOperationClaims.Admin,
        UsersOperationClaims.Read
    };

    public sealed class GetByIdUserQueryHandler : IRequestHandler<GetByIdUserQuery, UserDetailDto>
    {
        private readonly IUserAdministrationService _service;

        public GetByIdUserQueryHandler(IUserAdministrationService service)
        {
            _service = service;
        }

        public Task<UserDetailDto> Handle(GetByIdUserQuery request, CancellationToken cancellationToken)
        {
            return _service.GetByIdAsync(request.Id, request.Culture, cancellationToken);
        }
    }
}
