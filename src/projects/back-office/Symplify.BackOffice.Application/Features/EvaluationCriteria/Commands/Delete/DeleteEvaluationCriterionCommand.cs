using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.EvaluationCriteria.Constants;
using Symplify.BackOffice.Application.Features.EvaluationCriteria.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Application.Features.EvaluationCriteria.Commands.Delete;

public class DeleteEvaluationCriterionCommand : IRequest<DeletedEvaluationCriterionResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetEvaluationCriteria";

    public string[] Roles => new[] { EvaluationCriteriaOperationClaims.Admin, EvaluationCriteriaOperationClaims.Write, EvaluationCriteriaOperationClaims.Delete };

    public class DeleteEvaluationCriterionCommandHandler : IRequestHandler<DeleteEvaluationCriterionCommand, DeletedEvaluationCriterionResponse>
    {
        private readonly IEvaluationCriterionRepository _repository;
        private readonly IMapper _mapper;
        private readonly EvaluationCriterionBusinessRules _rules;

        public DeleteEvaluationCriterionCommandHandler(
            IEvaluationCriterionRepository repository,
            IMapper mapper,
            EvaluationCriterionBusinessRules rules)
        {
            _repository = repository;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<DeletedEvaluationCriterionResponse> Handle(DeleteEvaluationCriterionCommand request, CancellationToken cancellationToken)
        {
            EvaluationCriterion? entity = await _repository.GetAsync(predicate: item => item.Id.Equals(request.Id));
            await _rules.EvaluationCriterionShouldExistWhenSelected(entity);

            EvaluationCriterion deletedEntity = await _repository.DeleteAsync(entity!);
            await NormalizeVisibleOrdersAsync(request.Id, cancellationToken);

            return _mapper.Map<DeletedEvaluationCriterionResponse>(deletedEntity);
        }

        private async Task NormalizeVisibleOrdersAsync(Guid deletedEntityId, CancellationToken cancellationToken)
        {
            List<EvaluationCriterion> entities = _repository.Query()
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
