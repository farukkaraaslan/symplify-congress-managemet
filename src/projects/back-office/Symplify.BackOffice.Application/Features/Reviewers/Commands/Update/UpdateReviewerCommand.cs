using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.Reviewers.Constants;
using Symplify.BackOffice.Application.Features.Reviewers.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.Reviewers.Commands.Update;
public class UpdateReviewerCommand : IRequest<UpdatedReviewerResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Symplify.BackOffice.Domain.Enums.ReviewerStatus Status { get; set; }
    public bool IsActive { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetReviewers";
    public string[] Roles => new[] { ReviewersOperationClaims.Admin, ReviewersOperationClaims.Write, ReviewersOperationClaims.Update };
    public class UpdateReviewerCommandHandler : IRequestHandler<UpdateReviewerCommand, UpdatedReviewerResponse>
    {
        private readonly IReviewerRepository _repository; private readonly IMapper _mapper; private readonly ReviewerBusinessRules _rules;
        public UpdateReviewerCommandHandler(IReviewerRepository repository, IMapper mapper, ReviewerBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<UpdatedReviewerResponse> Handle(UpdateReviewerCommand request, CancellationToken cancellationToken)
        {
            Reviewer? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.ReviewerShouldExistWhenSelected(entity);
            entity!.UserId = request.UserId;
            entity!.Status = request.Status;
            entity!.IsActive = request.IsActive;
            Reviewer updatedEntity = await _repository.UpdateAsync(entity!);
            return _mapper.Map<UpdatedReviewerResponse>(updatedEntity);
        }
    }
}
