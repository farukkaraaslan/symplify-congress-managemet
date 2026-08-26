using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressEvaluationCriteria.Constants;
using Symplify.BackOffice.Application.Features.CongressEvaluationCriteria.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressEvaluationCriteria.Commands.Delete;
public class DeleteCongressEvaluationCriterionCommand : IRequest<DeletedCongressEvaluationCriterionResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressEvaluationCriteria";
    public string[] Roles => new[] { CongressEvaluationCriteriaOperationClaims.Admin, CongressEvaluationCriteriaOperationClaims.Write, CongressEvaluationCriteriaOperationClaims.Delete };
    public class DeleteCongressEvaluationCriterionCommandHandler : IRequestHandler<DeleteCongressEvaluationCriterionCommand, DeletedCongressEvaluationCriterionResponse>
    {
        private readonly ICongressEvaluationCriterionRepository _repository; private readonly IMapper _mapper; private readonly CongressEvaluationCriterionBusinessRules _rules;
        public DeleteCongressEvaluationCriterionCommandHandler(ICongressEvaluationCriterionRepository repository, IMapper mapper, CongressEvaluationCriterionBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedCongressEvaluationCriterionResponse> Handle(DeleteCongressEvaluationCriterionCommand request, CancellationToken cancellationToken)
        {
            CongressEvaluationCriterion? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.CongressEvaluationCriterionShouldExistWhenSelected(entity);
            CongressEvaluationCriterion deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedCongressEvaluationCriterionResponse>(deletedEntity);
        }
    }
}
