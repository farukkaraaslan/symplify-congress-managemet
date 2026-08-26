using AutoMapper;
using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.Reviewers.Constants;
using Symplify.BackOffice.Application.Features.Reviewers.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.Reviewers.Queries.GetById;
public class GetByIdReviewerQuery : IRequest<GetByIdReviewerResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { ReviewersOperationClaims.Admin, ReviewersOperationClaims.Read };
    public class GetByIdReviewerQueryHandler : IRequestHandler<GetByIdReviewerQuery, GetByIdReviewerResponse>
    {
        private readonly IReviewerRepository _repository; private readonly IMapper _mapper; private readonly ReviewerBusinessRules _rules;
        public GetByIdReviewerQueryHandler(IReviewerRepository repository, IMapper mapper, ReviewerBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<GetByIdReviewerResponse> Handle(GetByIdReviewerQuery request, CancellationToken cancellationToken)
        {
            Reviewer? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.ReviewerShouldExistWhenSelected(entity);
            return _mapper.Map<GetByIdReviewerResponse>(entity);
        }
    }
}
