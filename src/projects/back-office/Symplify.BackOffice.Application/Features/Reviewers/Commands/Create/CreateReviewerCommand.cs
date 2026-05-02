using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.Reviewers.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.Reviewers.Commands.Create;
public class CreateReviewerCommand : IRequest<CreatedReviewerResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid UserId { get; set; }
    public Symplify.BackOffice.Domain.Enums.ReviewerStatus Status { get; set; }
    public bool IsActive { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetReviewers";
    public string[] Roles => new[] { ReviewersOperationClaims.Admin, ReviewersOperationClaims.Write, ReviewersOperationClaims.Add };
    public class CreateReviewerCommandHandler : IRequestHandler<CreateReviewerCommand, CreatedReviewerResponse>
    {
        private readonly IReviewerRepository _repository;
        private readonly IMapper _mapper;
        public CreateReviewerCommandHandler(IReviewerRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<CreatedReviewerResponse> Handle(CreateReviewerCommand request, CancellationToken cancellationToken)
        {
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
