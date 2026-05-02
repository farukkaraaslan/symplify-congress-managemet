using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.Reviewers.Constants;
using Symplify.BackOffice.Application.Features.Reviewers.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.Reviewers.Commands.Delete;
public class DeleteReviewerCommand : IRequest<DeletedReviewerResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetReviewers";
    public string[] Roles => new[] { ReviewersOperationClaims.Admin, ReviewersOperationClaims.Write, ReviewersOperationClaims.Delete };
    public class DeleteReviewerCommandHandler : IRequestHandler<DeleteReviewerCommand, DeletedReviewerResponse>
    {
        private readonly IReviewerRepository _repository; private readonly IMapper _mapper; private readonly ReviewerBusinessRules _rules;
        public DeleteReviewerCommandHandler(IReviewerRepository repository, IMapper mapper, ReviewerBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedReviewerResponse> Handle(DeleteReviewerCommand request, CancellationToken cancellationToken)
        {
            Reviewer? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.ReviewerShouldExistWhenSelected(entity);
            Reviewer deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedReviewerResponse>(deletedEntity);
        }
    }
}
