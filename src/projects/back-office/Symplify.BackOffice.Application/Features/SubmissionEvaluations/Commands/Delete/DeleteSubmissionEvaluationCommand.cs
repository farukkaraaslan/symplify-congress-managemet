using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.SubmissionEvaluations.Constants;
using Symplify.BackOffice.Application.Features.SubmissionEvaluations.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.SubmissionEvaluations.Commands.Delete;
public class DeleteSubmissionEvaluationCommand : IRequest<DeletedSubmissionEvaluationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetSubmissionEvaluations";
    public string[] Roles => new[] { SubmissionEvaluationsOperationClaims.Admin, SubmissionEvaluationsOperationClaims.Write, SubmissionEvaluationsOperationClaims.Delete };
    public class DeleteSubmissionEvaluationCommandHandler : IRequestHandler<DeleteSubmissionEvaluationCommand, DeletedSubmissionEvaluationResponse>
    {
        private readonly ISubmissionEvaluationRepository _repository; private readonly IMapper _mapper; private readonly SubmissionEvaluationBusinessRules _rules;
        public DeleteSubmissionEvaluationCommandHandler(ISubmissionEvaluationRepository repository, IMapper mapper, SubmissionEvaluationBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedSubmissionEvaluationResponse> Handle(DeleteSubmissionEvaluationCommand request, CancellationToken cancellationToken)
        {
            SubmissionEvaluation? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.SubmissionEvaluationShouldExistWhenSelected(entity);
            SubmissionEvaluation deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedSubmissionEvaluationResponse>(deletedEntity);
        }
    }
}
