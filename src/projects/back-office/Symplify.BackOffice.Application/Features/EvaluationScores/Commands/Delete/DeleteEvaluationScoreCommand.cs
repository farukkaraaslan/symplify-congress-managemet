using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.EvaluationScores.Constants;
using Symplify.BackOffice.Application.Features.EvaluationScores.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.EvaluationScores.Commands.Delete;
public class DeleteEvaluationScoreCommand : IRequest<DeletedEvaluationScoreResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetEvaluationScores";
    public string[] Roles => new[] { EvaluationScoresOperationClaims.Admin, EvaluationScoresOperationClaims.Write, EvaluationScoresOperationClaims.Delete };
    public class DeleteEvaluationScoreCommandHandler : IRequestHandler<DeleteEvaluationScoreCommand, DeletedEvaluationScoreResponse>
    {
        private readonly IEvaluationScoreRepository _repository; private readonly IMapper _mapper; private readonly EvaluationScoreBusinessRules _rules;
        public DeleteEvaluationScoreCommandHandler(IEvaluationScoreRepository repository, IMapper mapper, EvaluationScoreBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedEvaluationScoreResponse> Handle(DeleteEvaluationScoreCommand request, CancellationToken cancellationToken)
        {
            EvaluationScore? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.EvaluationScoreShouldExistWhenSelected(entity);
            EvaluationScore deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedEvaluationScoreResponse>(deletedEntity);
        }
    }
}
