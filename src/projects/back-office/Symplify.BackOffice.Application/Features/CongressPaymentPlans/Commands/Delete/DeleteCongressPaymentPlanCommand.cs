using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressPaymentPlans.Constants;
using Symplify.BackOffice.Application.Features.CongressPaymentPlans.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressPaymentPlans.Commands.Delete;

public class DeleteCongressPaymentPlanCommand : IRequest<DeletedCongressPaymentPlanResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressPaymentPlans";
    public string[] Roles => new[] { CongressPaymentPlansOperationClaims.Admin, CongressPaymentPlansOperationClaims.Write, CongressPaymentPlansOperationClaims.Delete };

    public class DeleteCongressPaymentPlanCommandHandler : IRequestHandler<DeleteCongressPaymentPlanCommand, DeletedCongressPaymentPlanResponse>
    {
        private readonly ICongressPaymentPlanRepository _repository;
        private readonly IMapper _mapper;
        private readonly CongressPaymentPlanBusinessRules _rules;

        public DeleteCongressPaymentPlanCommandHandler(
            ICongressPaymentPlanRepository repository,
            IMapper mapper,
            CongressPaymentPlanBusinessRules rules)
        {
            _repository = repository;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<DeletedCongressPaymentPlanResponse> Handle(DeleteCongressPaymentPlanCommand request, CancellationToken cancellationToken)
        {
            CongressPaymentPlan? entity = await _repository.GetAsync(predicate: x => x.Id.Equals(request.Id));
            await _rules.CongressPaymentPlanShouldExistWhenSelected(entity);

            Guid congressId = entity!.CongressId;
            CongressPaymentPlan deletedEntity = await _repository.DeleteAsync(entity);

            await NormalizeVisibleOrdersAsync(congressId, request.Id, cancellationToken);

            return _mapper.Map<DeletedCongressPaymentPlanResponse>(deletedEntity);
        }

        private async Task NormalizeVisibleOrdersAsync(Guid congressId, Guid deletedEntityId, CancellationToken cancellationToken)
        {
            List<CongressPaymentPlan> entities = _repository.Query()
                .ToList()
                .Where(entity => entity.CongressId == congressId && entity.Id != deletedEntityId && !IsDeleted(entity))
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
