using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressEvaluationCriteria.Constants;
using Symplify.BackOffice.Application.Features.CongressEvaluationCriteria.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressEvaluationCriteria.Commands.Update;
public class UpdateCongressEvaluationCriterionCommand : IRequest<UpdatedCongressEvaluationCriterionResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public Guid EvaluationCriterionId { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressEvaluationCriteria";
    public string[] Roles => new[] { CongressEvaluationCriteriaOperationClaims.Admin, CongressEvaluationCriteriaOperationClaims.Write, CongressEvaluationCriteriaOperationClaims.Update };
    public class UpdateCongressEvaluationCriterionCommandHandler : IRequestHandler<UpdateCongressEvaluationCriterionCommand, UpdatedCongressEvaluationCriterionResponse>
    {
        private readonly ICongressEvaluationCriterionRepository _repository; private readonly IMapper _mapper; private readonly CongressEvaluationCriterionBusinessRules _rules;
        public UpdateCongressEvaluationCriterionCommandHandler(ICongressEvaluationCriterionRepository repository, IMapper mapper, CongressEvaluationCriterionBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<UpdatedCongressEvaluationCriterionResponse> Handle(UpdateCongressEvaluationCriterionCommand request, CancellationToken cancellationToken)
        {
            CongressEvaluationCriterion? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.CongressEvaluationCriterionShouldExistWhenSelected(entity);
            entity!.CongressId = request.CongressId;
            entity!.EvaluationCriterionId = request.EvaluationCriterionId;
            entity!.Order = request.Order;
            entity!.IsActive = request.IsActive;
            CongressEvaluationCriterion updatedEntity = await _repository.UpdateAsync(entity!);
            return _mapper.Map<UpdatedCongressEvaluationCriterionResponse>(updatedEntity);
        }
    }
}
