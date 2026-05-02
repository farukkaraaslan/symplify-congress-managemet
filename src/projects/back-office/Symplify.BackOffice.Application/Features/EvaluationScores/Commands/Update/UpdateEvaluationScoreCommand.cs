using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.EvaluationScores.Constants;
using Symplify.BackOffice.Application.Features.EvaluationScores.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.EvaluationScores.Commands.Update;
public class UpdateEvaluationScoreCommand : IRequest<UpdatedEvaluationScoreResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public Guid SubmissionEvaluationId { get; set; }
    public Guid EvaluationCriterionId { get; set; }
    public decimal Score { get; set; }
    public string? Comment { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetEvaluationScores";
    public string[] Roles => new[] { EvaluationScoresOperationClaims.Admin, EvaluationScoresOperationClaims.Write, EvaluationScoresOperationClaims.Update };
    public class UpdateEvaluationScoreCommandHandler : IRequestHandler<UpdateEvaluationScoreCommand, UpdatedEvaluationScoreResponse>
    {
        private readonly IEvaluationScoreRepository _repository; private readonly IMapper _mapper; private readonly EvaluationScoreBusinessRules _rules;
        public UpdateEvaluationScoreCommandHandler(IEvaluationScoreRepository repository, IMapper mapper, EvaluationScoreBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<UpdatedEvaluationScoreResponse> Handle(UpdateEvaluationScoreCommand request, CancellationToken cancellationToken)
        {
            EvaluationScore? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.EvaluationScoreShouldExistWhenSelected(entity);
            entity!.SubmissionEvaluationId = request.SubmissionEvaluationId;
            entity!.EvaluationCriterionId = request.EvaluationCriterionId;
            entity!.Score = request.Score;
            entity!.Comment = request.Comment;
            EvaluationScore updatedEntity = await _repository.UpdateAsync(entity!);
            return _mapper.Map<UpdatedEvaluationScoreResponse>(updatedEntity);
        }
    }
}
