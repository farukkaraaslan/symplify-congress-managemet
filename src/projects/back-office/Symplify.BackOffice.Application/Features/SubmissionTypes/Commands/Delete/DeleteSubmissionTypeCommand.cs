using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.SubmissionTypes.Constants;
using Symplify.BackOffice.Application.Features.SubmissionTypes.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Application.Features.SubmissionTypes.Commands.Delete;

public class DeleteSubmissionTypeCommand : IRequest<DeletedSubmissionTypeResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetSubmissionTypes";

    public string[] Roles => new[] { SubmissionTypesOperationClaims.Admin, SubmissionTypesOperationClaims.Write, SubmissionTypesOperationClaims.Delete };

    public class DeleteSubmissionTypeCommandHandler : IRequestHandler<DeleteSubmissionTypeCommand, DeletedSubmissionTypeResponse>
    {
        private readonly ISubmissionTypeRepository _repository;
        private readonly IMapper _mapper;
        private readonly SubmissionTypeBusinessRules _rules;

        public DeleteSubmissionTypeCommandHandler(
            ISubmissionTypeRepository repository,
            IMapper mapper,
            SubmissionTypeBusinessRules rules)
        {
            _repository = repository;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<DeletedSubmissionTypeResponse> Handle(DeleteSubmissionTypeCommand request, CancellationToken cancellationToken)
        {
            SubmissionType? entity = await _repository.GetAsync(predicate: item => item.Id.Equals(request.Id));
            await _rules.SubmissionTypeShouldExistWhenSelected(entity);

            SubmissionType deletedEntity = await _repository.DeleteAsync(entity!);
            await NormalizeVisibleOrdersAsync(request.Id, cancellationToken);

            return _mapper.Map<DeletedSubmissionTypeResponse>(deletedEntity);
        }

        private async Task NormalizeVisibleOrdersAsync(Guid deletedEntityId, CancellationToken cancellationToken)
        {
            List<SubmissionType> entities = _repository.Query()
                .ToList()
                .Where(entity => !IsDeleted(entity) && entity.Id != deletedEntityId)
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.Id)
                .ToList();

            for (int index = 0; index < entities.Count; index++)
            {
                int normalizedOrder = index + 1;

                if (entities[index].Order == normalizedOrder)
                    continue;

                entities[index].Order = normalizedOrder;
                await _repository.UpdateAsync(entities[index]);
            }
        }

        private static bool IsDeleted(object entity)
        {
            object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
            return deletedDate is not null;
        }
    }
}
