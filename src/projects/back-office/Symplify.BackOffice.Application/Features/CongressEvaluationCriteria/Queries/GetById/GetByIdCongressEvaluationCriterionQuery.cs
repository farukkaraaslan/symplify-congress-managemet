using AutoMapper;
using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressEvaluationCriteria.Constants;
using Symplify.BackOffice.Application.Features.CongressEvaluationCriteria.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressEvaluationCriteria.Queries.GetById;
public class GetByIdCongressEvaluationCriterionQuery : IRequest<GetByIdCongressEvaluationCriterionResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { CongressEvaluationCriteriaOperationClaims.Admin, CongressEvaluationCriteriaOperationClaims.Read };
    public class GetByIdCongressEvaluationCriterionQueryHandler : IRequestHandler<GetByIdCongressEvaluationCriterionQuery, GetByIdCongressEvaluationCriterionResponse>
    {
        private readonly ICongressEvaluationCriterionRepository _repository; private readonly IMapper _mapper; private readonly CongressEvaluationCriterionBusinessRules _rules;
        public GetByIdCongressEvaluationCriterionQueryHandler(ICongressEvaluationCriterionRepository repository, IMapper mapper, CongressEvaluationCriterionBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<GetByIdCongressEvaluationCriterionResponse> Handle(GetByIdCongressEvaluationCriterionQuery request, CancellationToken cancellationToken)
        {
            CongressEvaluationCriterion? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.CongressEvaluationCriterionShouldExistWhenSelected(entity);
            return _mapper.Map<GetByIdCongressEvaluationCriterionResponse>(entity);
        }
    }
}
