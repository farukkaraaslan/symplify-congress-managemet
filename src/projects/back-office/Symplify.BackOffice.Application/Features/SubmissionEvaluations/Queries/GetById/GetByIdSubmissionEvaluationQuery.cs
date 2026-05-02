using AutoMapper;
using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.SubmissionEvaluations.Constants;
using Symplify.BackOffice.Application.Features.SubmissionEvaluations.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.SubmissionEvaluations.Queries.GetById;
public class GetByIdSubmissionEvaluationQuery : IRequest<GetByIdSubmissionEvaluationResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { SubmissionEvaluationsOperationClaims.Admin, SubmissionEvaluationsOperationClaims.Read };
    public class GetByIdSubmissionEvaluationQueryHandler : IRequestHandler<GetByIdSubmissionEvaluationQuery, GetByIdSubmissionEvaluationResponse>
    {
        private readonly ISubmissionEvaluationRepository _repository; private readonly IMapper _mapper; private readonly SubmissionEvaluationBusinessRules _rules;
        public GetByIdSubmissionEvaluationQueryHandler(ISubmissionEvaluationRepository repository, IMapper mapper, SubmissionEvaluationBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<GetByIdSubmissionEvaluationResponse> Handle(GetByIdSubmissionEvaluationQuery request, CancellationToken cancellationToken)
        {
            SubmissionEvaluation? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.SubmissionEvaluationShouldExistWhenSelected(entity);
            return _mapper.Map<GetByIdSubmissionEvaluationResponse>(entity);
        }
    }
}
