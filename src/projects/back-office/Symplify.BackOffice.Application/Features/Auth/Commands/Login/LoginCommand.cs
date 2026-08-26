using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.Auth.Constants;
using Symplify.BackOffice.Application.Services.Authentication;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;

namespace Symplify.BackOffice.Application.Features.Auth.Commands.Login;

public sealed class LoginCommand : IRequest<LoggedInResponse>
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public Guid? OrganizationId { get; set; }

    public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoggedInResponse>
    {
        private readonly IBackOfficeIdentityService _identityService;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IOrganizationUserRepository _organizationUserRepository;

        public LoginCommandHandler(
            IBackOfficeIdentityService identityService,
            IOrganizationRepository organizationRepository,
            IOrganizationUserRepository organizationUserRepository)
        {
            _identityService = identityService;
            _organizationRepository = organizationRepository;
            _organizationUserRepository = organizationUserRepository;
        }

        public async Task<LoggedInResponse> Handle(
            LoginCommand request,
            CancellationToken cancellationToken)
        {
            AuthenticatedUserDto user = await _identityService.AuthenticateAsync(
                request.Email,
                request.Password,
                cancellationToken);

            if (request.OrganizationId.HasValue && request.OrganizationId.Value != Guid.Empty)
            {
                await AttachOrganizationContextAsync(
                    user,
                    request.OrganizationId.Value,
                    skipMembershipCheck: IsOrganizationContextExempt(user),
                    cancellationToken);
            }

            return new LoggedInResponse
            {
                User = user
            };
        }

        private static bool IsOrganizationContextExempt(AuthenticatedUserDto user)
        {
            return user.OperationClaims.Any(claim =>
                string.Equals(claim, "SuperAdmin", StringComparison.OrdinalIgnoreCase));
        }

        private async Task AttachOrganizationContextAsync(
            AuthenticatedUserDto user,
            Guid organizationId,
            bool skipMembershipCheck,
            CancellationToken cancellationToken)
        {
            Organization? organization = await _organizationRepository.GetAsync(
                predicate: item => item.Id == organizationId && item.IsActive && item.DeletedDate == null,
                cancellationToken: cancellationToken);

            if (organization is null)
                throw new BusinessException(AuthMessages.OrganizationNotFound);

            if (!skipMembershipCheck)
            {
                bool hasMembership = _organizationUserRepository
                    .Query()
                    .Any(item =>
                        item.UserId == user.Id &&
                        item.OrganizationId == organizationId &&
                        item.IsActive &&
                        item.DeletedDate == null);

                if (!hasMembership)
                    throw new BusinessException(AuthMessages.OrganizationMembershipRequired);
            }

            user.OrganizationId = organization.Id;
            user.OrganizationSlug = organization.Slug;
            user.OrganizationName = organization.Name;
            user.OrganizationShortName = organization.ShortName;
        }
    }
}
