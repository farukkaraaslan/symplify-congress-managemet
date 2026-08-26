using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Common.Text;
using Symplify.BackOffice.Application.Features.Auth.Constants;
using Symplify.BackOffice.Application.Services.Authentication;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Organization;
using CongressEntity = Symplify.BackOffice.Domain.Congress.Congress;

namespace Symplify.BackOffice.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommand : IRequest<RegisteredResponse>
{
    public string Name { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;

    public string Institution { get; set; } = string.Empty;

    public Guid? TitleId { get; set; }

    public Guid? OrganizationId { get; set; }

    public Guid? CountryId { get; set; }

    public Guid? StateId { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string ConfirmPassword { get; set; } = string.Empty;

    public bool AcceptTerms { get; set; }

    public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisteredResponse>
    {
        private readonly IBackOfficeIdentityService _identityService;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly ICongressRepository _congressRepository;
        private readonly IOrganizationUserRepository _organizationUserRepository;

        public RegisterCommandHandler(
            IBackOfficeIdentityService identityService,
            IOrganizationRepository organizationRepository,
            ICongressRepository congressRepository,
            IOrganizationUserRepository organizationUserRepository)
        {
            _identityService = identityService;
            _organizationRepository = organizationRepository;
            _congressRepository = congressRepository;
            _organizationUserRepository = organizationUserRepository;
        }

        public async Task<RegisteredResponse> Handle(
            RegisterCommand request,
            CancellationToken cancellationToken)
        {
            if (!request.OrganizationId.HasValue || request.OrganizationId.Value == Guid.Empty)
                throw new BusinessException(AuthMessages.OrganizationRequired);

            Organization? organization = await _organizationRepository.GetAsync(
                predicate: item => item.Id == request.OrganizationId.Value && item.IsActive && item.DeletedDate == null,
                cancellationToken: cancellationToken);

            if (organization is null)
                throw new BusinessException(AuthMessages.OrganizationNotFound);

            CongressEntity? publishedCongress = ResolvePublishedCongress(organization.Id);

            RegisteredAuthorUserDto registeredUser = await _identityService.RegisterAuthorAsync(
                new RegisterAuthorUserRequest
                {
                    Name = BackOfficeTextNormalizer.NormalizeRequiredPersonFirstName(request.Name),
                    Surname = BackOfficeTextNormalizer.NormalizeRequiredPersonSurname(request.Surname),
                    Institution = BackOfficeTextNormalizer.NormalizeInstitution(request.Institution) ?? string.Empty,
                    TitleId = request.TitleId,
                    CountryId = request.CountryId,
                    StateId = request.StateId,
                    PhoneNumber = request.PhoneNumber,
                    Email = request.Email,
                    Password = request.Password
                },
                cancellationToken);

            OrganizationUser organizationUser = new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                UserId = registeredUser.UserId,
                DefaultCongressId = publishedCongress?.Id,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "SelfRegistration"
            };

            await _organizationUserRepository.AddAsync(organizationUser);

            return new RegisteredResponse
            {
                UserId = registeredUser.UserId,
                Email = registeredUser.Email,
                DisplayName = registeredUser.DisplayName,
                EmailConfirmationToken = registeredUser.EmailConfirmationToken,
                OrganizationId = organization.Id,
                OrganizationName = organization.Name,
                OrganizationShortName = organization.ShortName,
                OrganizationSlug = organization.Slug,
                OrganizationLogoLightPath = organization.LogoLightPath
            };
        }

        private CongressEntity? ResolvePublishedCongress(Guid organizationId)
        {
            return _congressRepository
                .Query()
                .Where(item =>
                    item.OrganizationId == organizationId &&
                    item.Status == CongressStatus.Published &&
                    item.DeletedDate == null)
                .OrderByDescending(item => item.StartDate)
                .ThenByDescending(item => item.CreatedDate)
                .FirstOrDefault();
        }
    }
}
