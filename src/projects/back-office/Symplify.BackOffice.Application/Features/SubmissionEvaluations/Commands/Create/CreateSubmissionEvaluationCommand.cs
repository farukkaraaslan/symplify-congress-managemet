using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.SubmissionEvaluations.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.SubmissionEvaluations.Commands.Create;
public class CreateSubmissionEvaluationCommand : IRequest<CreatedSubmissionEvaluationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid SubmissionId { get; set; }
    public Guid ReviewerId { get; set; }
    public string? Comment { get; set; }
    public string? Recommendation { get; set; }
    public decimal? TotalScore { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetSubmissionEvaluations";
    public string[] Roles => new[] { SubmissionEvaluationsOperationClaims.Admin, SubmissionEvaluationsOperationClaims.Write, SubmissionEvaluationsOperationClaims.Add };
    public class CreateSubmissionEvaluationCommandHandler : IRequestHandler<CreateSubmissionEvaluationCommand, CreatedSubmissionEvaluationResponse>
    {
        private readonly ISubmissionEvaluationRepository _repository;
        private readonly IMapper _mapper;
        public CreateSubmissionEvaluationCommandHandler(ISubmissionEvaluationRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<CreatedSubmissionEvaluationResponse> Handle(CreateSubmissionEvaluationCommand request, CancellationToken cancellationToken)
        {
            SubmissionEvaluation entity = new()
            {
                Id = Guid.NewGuid(),
                SubmissionId = request.SubmissionId,
                ReviewerId = request.ReviewerId,
                Comment = request.Comment,
                Recommendation = request.Recommendation,
                TotalScore = request.TotalScore,
                CompletedAt = request.CompletedAt,
            };
            SubmissionEvaluation createdEntity = await _repository.AddAsync(entity);
            return _mapper.Map<CreatedSubmissionEvaluationResponse>(createdEntity);
        }
    }
}
