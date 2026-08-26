using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Storage;
using MediatR;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Common.Storage;
using Symplify.BackOffice.Application.Features.CongressSliders.Constants;
using Symplify.BackOffice.Application.Features.CongressSliders.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressSliders.Commands.Delete;

public class DeleteCongressSliderCommand : IRequest<DeletedCongressSliderResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressSliders";
    public string[] Roles => new[] { CongressSlidersOperationClaims.Admin, CongressSlidersOperationClaims.Write, CongressSlidersOperationClaims.Delete };

    public class DeleteCongressSliderCommandHandler : IRequestHandler<DeleteCongressSliderCommand, DeletedCongressSliderResponse>
    {
        private readonly ICongressSliderRepository _repository;
        private readonly IObjectStorageService _objectStorageService;
        private readonly ObjectStorageOptions _storageOptions;
        private readonly IMapper _mapper;
        private readonly CongressSliderBusinessRules _rules;

        public DeleteCongressSliderCommandHandler(
            ICongressSliderRepository repository,
            IObjectStorageService objectStorageService,
            IOptions<ObjectStorageOptions> storageOptions,
            IMapper mapper,
            CongressSliderBusinessRules rules)
        {
            _repository = repository;
            _objectStorageService = objectStorageService;
            _storageOptions = storageOptions.Value;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<DeletedCongressSliderResponse> Handle(DeleteCongressSliderCommand request, CancellationToken cancellationToken)
        {
            CongressSlider? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.CongressSliderShouldExistWhenSelected(entity);

            string? imagePath = entity!.ImagePath;
            CongressSlider deletedEntity = await _repository.DeleteAsync(entity);

            await BackOfficeObjectStorageHelper.DeleteObjectIfExistsAsync(
                _objectStorageService,
                GetCongressImagesBucketName(),
                imagePath,
                cancellationToken);

            return _mapper.Map<DeletedCongressSliderResponse>(deletedEntity);
        }

        private string GetCongressImagesBucketName()
        {
            if (string.IsNullOrWhiteSpace(_storageOptions.Buckets.CongressImages))
                throw new InvalidOperationException(CongressSlidersMessages.ObjectStorageBucketMissing);

            return _storageOptions.Buckets.CongressImages.Trim();
        }
    }
}
