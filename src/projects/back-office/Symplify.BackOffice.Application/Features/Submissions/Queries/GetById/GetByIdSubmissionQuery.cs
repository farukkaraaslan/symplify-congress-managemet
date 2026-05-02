using AutoMapper;
using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.Submissions.Constants;
using Symplify.BackOffice.Application.Features.Submissions.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.Submissions.Queries.GetById;
public class GetByIdSubmissionQuery : IRequest<GetByIdSubmissionResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { SubmissionsOperationClaims.Admin, SubmissionsOperationClaims.Read };
    public class GetByIdSubmissionQueryHandler : IRequestHandler<GetByIdSubmissionQuery, GetByIdSubmissionResponse>
    {
        private readonly ISubmissionRepository _repository; private readonly IMapper _mapper; private readonly SubmissionBusinessRules _rules;
        public GetByIdSubmissionQueryHandler(ISubmissionRepository repository, IMapper mapper, SubmissionBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<GetByIdSubmissionResponse> Handle(GetByIdSubmissionQuery request, CancellationToken cancellationToken)
        {
            Submission? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.SubmissionShouldExistWhenSelected(entity);
            return _mapper.Map<GetByIdSubmissionResponse>(entity);
        }
    }
}
