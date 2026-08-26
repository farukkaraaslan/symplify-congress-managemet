using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Storage;
using MediatR;
using Symplify.BackOffice.Application.Common.Storage;
using Symplify.BackOffice.Application.Features.OrganizationMailConfigurations.Rules;
using Symplify.BackOffice.Application.Features.Organizations.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;

namespace Symplify.BackOffice.Application.Features.OrganizationMailConfigurations.Commands.Delete;

public sealed class DeleteOrganizationMailConfigurationCommand :
    IRequest<DeleteOrganizationMailConfigurationResponse>,
    ISecuredRequest,
    ICacheRemoverRequest
{
    public Guid OrganizationId { get; set; }

    public string[] Roles =>
    [
        OrganizationsOperationClaims.Admin,
        OrganizationsOperationClaims.Delete
    ];

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => $"OrganizationMailConfiguration({OrganizationId})";

    public sealed class Handler : IRequestHandler<DeleteOrganizationMailConfigurationCommand, DeleteOrganizationMailConfigurationResponse>
    {
        private readonly IOrganizationMailConfigurationRepository _repository;
        private readonly IObjectStorageService _objectStorageService;
        private readonly OrganizationMailConfigurationBusinessRules _rules;

        public Handler(
            IOrganizationMailConfigurationRepository repository,
            IObjectStorageService objectStorageService,
            OrganizationMailConfigurationBusinessRules rules)
        {
            _repository = repository;
            _objectStorageService = objectStorageService;
            _rules = rules;
        }

        public async Task<DeleteOrganizationMailConfigurationResponse> Handle(
            DeleteOrganizationMailConfigurationCommand request,
            CancellationToken cancellationToken)
        {
            OrganizationMailConfiguration entity = await _rules.ConfigurationShouldExistAsync(
                request.OrganizationId,
                cancellationToken);

            string? logoBucketName = entity.MailLogoBucketName;
            string? logoObjectName = entity.MailLogoObjectName;

            entity.IsActive = false;
            entity.DeletedDate = DateTime.UtcNow;
            entity.DeletedBy = "OrganizationMailConfiguration";
            entity.UpdatedDate = DateTime.UtcNow;
            entity.UpdatedBy = "OrganizationMailConfiguration";

            await _repository.UpdateAsync(entity);

            await BackOfficeObjectStorageHelper.DeleteObjectIfExistsAsync(
                _objectStorageService,
                logoBucketName,
                logoObjectName,
                cancellationToken);

            return new DeleteOrganizationMailConfigurationResponse
            {
                Id = entity.Id,
                OrganizationId = entity.OrganizationId
            };
        }
    }
}
