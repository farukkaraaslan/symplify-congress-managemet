using AutoMapper;
using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.EvaluationScores.Constants;
using Symplify.BackOffice.Application.Features.EvaluationScores.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.EvaluationScores.Queries.GetById;
public class GetByIdEvaluationScoreQuery : IRequest<GetByIdEvaluationScoreResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { EvaluationScoresOperationClaims.Admin, EvaluationScoresOperationClaims.Read };
    public class GetByIdEvaluationScoreQueryHandler : IRequestHandler<GetByIdEvaluationScoreQuery, GetByIdEvaluationScoreResponse>
    {
        private readonly IEvaluationScoreRepository _repository; private readonly IMapper _mapper; private readonly EvaluationScoreBusinessRules _rules;
        public GetByIdEvaluationScoreQueryHandler(IEvaluationScoreRepository repository, IMapper mapper, EvaluationScoreBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<GetByIdEvaluationScoreResponse> Handle(GetByIdEvaluationScoreQuery request, CancellationToken cancellationToken)
        {
            EvaluationScore? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.EvaluationScoreShouldExistWhenSelected(entity);
            return _mapper.Map<GetByIdEvaluationScoreResponse>(entity);
        }
    }
}
