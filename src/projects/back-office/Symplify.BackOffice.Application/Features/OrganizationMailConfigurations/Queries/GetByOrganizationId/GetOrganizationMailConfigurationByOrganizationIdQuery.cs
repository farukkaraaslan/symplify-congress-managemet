using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.Organizations.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;

namespace Symplify.BackOffice.Application.Features.OrganizationMailConfigurations.Queries.GetByOrganizationId;

public sealed class GetOrganizationMailConfigurationByOrganizationIdQuery :
    IRequest<GetOrganizationMailConfigurationByOrganizationIdResponse>,
    ISecuredRequest
{
    public Guid OrganizationId { get; set; }

    public string[] Roles =>
    [
        OrganizationsOperationClaims.Admin,
        OrganizationsOperationClaims.Read
    ];

    public sealed class Handler : IRequestHandler<GetOrganizationMailConfigurationByOrganizationIdQuery, GetOrganizationMailConfigurationByOrganizationIdResponse>
    {
        private readonly IOrganizationMailConfigurationRepository _repository;

        public Handler(IOrganizationMailConfigurationRepository repository)
        {
            _repository = repository;
        }

        public async Task<GetOrganizationMailConfigurationByOrganizationIdResponse> Handle(
            GetOrganizationMailConfigurationByOrganizationIdQuery request,
            CancellationToken cancellationToken)
        {
            OrganizationMailConfiguration? entity = await _repository.GetAsync(
                predicate: item => item.OrganizationId == request.OrganizationId,
                cancellationToken: cancellationToken);

            if (entity is null)
            {
                return new GetOrganizationMailConfigurationByOrganizationIdResponse
                {
                    OrganizationId = request.OrganizationId,
                    Port = 587,
                    EnableSsl = true,
                    IsActive = true,
                    Exists = false
                };
            }

            return new GetOrganizationMailConfigurationByOrganizationIdResponse
            {
                Id = entity.Id,
                OrganizationId = entity.OrganizationId,
                Host = entity.Host,
                Port = entity.Port,
                EnableSsl = entity.EnableSsl,
                Username = entity.Username,
                FromEmail = entity.FromEmail,
                FromName = entity.FromName,
                ReplyToEmail = entity.ReplyToEmail,
                ReplyToName = entity.ReplyToName,
                MailLogoBucketName = entity.MailLogoBucketName,
                MailLogoObjectName = entity.MailLogoObjectName,
                MailLogoContentType = entity.MailLogoContentType,
                MailLogoFileName = entity.MailLogoFileName,
                HasMailLogo = !string.IsNullOrWhiteSpace(entity.MailLogoBucketName) &&
                              !string.IsNullOrWhiteSpace(entity.MailLogoObjectName),
                IsActive = entity.IsActive,
                HasStoredPassword = !string.IsNullOrWhiteSpace(entity.PasswordCipherText),
                LastTestedAt = entity.LastTestedAt,
                LastTestSucceeded = entity.LastTestSucceeded,
                LastTestError = entity.LastTestError,
                Exists = true
            };
        }
    }
}
