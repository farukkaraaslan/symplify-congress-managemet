using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.Reviewers.Constants;
using Symplify.BackOffice.Application.Features.Reviewers.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Application.Features.Reviewers.Commands.Create;

public class CreateReviewerCommand : IRequest<CreatedReviewerResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid UserId { get; set; }
    public ReviewerStatus Status { get; set; } = ReviewerStatus.Accepted;
    public bool IsActive { get; set; } = true;
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetReviewers";
    public string[] Roles => new[] { ReviewersOperationClaims.Admin, ReviewersOperationClaims.Write, ReviewersOperationClaims.Add };

    public class CreateReviewerCommandHandler : IRequestHandler<CreateReviewerCommand, CreatedReviewerResponse>
    {
        private readonly IReviewerRepository _repository;
        private readonly IMapper _mapper;
        private readonly ReviewerBusinessRules _rules;

        public CreateReviewerCommandHandler(IReviewerRepository repository, IMapper mapper, ReviewerBusinessRules rules)
        {
            _repository = repository;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<CreatedReviewerResponse> Handle(CreateReviewerCommand request, CancellationToken cancellationToken)
        {
            await _rules.ReviewerShouldNotExistForUser(request.UserId);

            Reviewer entity = new()
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Status = request.Status,
                IsActive = request.IsActive,
            };

            Reviewer createdEntity = await _repository.AddAsync(entity);
            return _mapper.Map<CreatedReviewerResponse>(createdEntity);
        }
    }
}
