using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.SubmissionEvaluations.Constants;
using Symplify.BackOffice.Application.Features.SubmissionEvaluations.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.SubmissionEvaluations.Commands.Update;
public class UpdateSubmissionEvaluationCommand : IRequest<UpdatedSubmissionEvaluationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid ReviewerId { get; set; }
    public string? Comment { get; set; }
    public string? Recommendation { get; set; }
    public decimal? TotalScore { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetSubmissionEvaluations";
    public string[] Roles => new[] { SubmissionEvaluationsOperationClaims.Admin, SubmissionEvaluationsOperationClaims.Write, SubmissionEvaluationsOperationClaims.Update };
    public class UpdateSubmissionEvaluationCommandHandler : IRequestHandler<UpdateSubmissionEvaluationCommand, UpdatedSubmissionEvaluationResponse>
    {
        private readonly ISubmissionEvaluationRepository _repository; private readonly IMapper _mapper; private readonly SubmissionEvaluationBusinessRules _rules;
        public UpdateSubmissionEvaluationCommandHandler(ISubmissionEvaluationRepository repository, IMapper mapper, SubmissionEvaluationBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<UpdatedSubmissionEvaluationResponse> Handle(UpdateSubmissionEvaluationCommand request, CancellationToken cancellationToken)
        {
            SubmissionEvaluation? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.SubmissionEvaluationShouldExistWhenSelected(entity);
            entity!.SubmissionId = request.SubmissionId;
            entity!.ReviewerId = request.ReviewerId;
            entity!.Comment = request.Comment;
            entity!.Recommendation = request.Recommendation;
            entity!.TotalScore = request.TotalScore;
            entity!.CompletedAt = request.CompletedAt;
            SubmissionEvaluation updatedEntity = await _repository.UpdateAsync(entity!);
            return _mapper.Map<UpdatedSubmissionEvaluationResponse>(updatedEntity);
        }
    }
}
