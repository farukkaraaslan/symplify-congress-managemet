using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.EvaluationScores.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.EvaluationScores.Commands.Create;
public class CreateEvaluationScoreCommand : IRequest<CreatedEvaluationScoreResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid SubmissionEvaluationId { get; set; }
    public Guid EvaluationCriterionId { get; set; }
    public decimal Score { get; set; }
    public string? Comment { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetEvaluationScores";
    public string[] Roles => new[] { EvaluationScoresOperationClaims.Admin, EvaluationScoresOperationClaims.Write, EvaluationScoresOperationClaims.Add };
    public class CreateEvaluationScoreCommandHandler : IRequestHandler<CreateEvaluationScoreCommand, CreatedEvaluationScoreResponse>
    {
        private readonly IEvaluationScoreRepository _repository;
        private readonly IMapper _mapper;
        public CreateEvaluationScoreCommandHandler(IEvaluationScoreRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<CreatedEvaluationScoreResponse> Handle(CreateEvaluationScoreCommand request, CancellationToken cancellationToken)
        {
            EvaluationScore entity = new()
            {
                Id = Guid.NewGuid(),
                SubmissionEvaluationId = request.SubmissionEvaluationId,
                EvaluationCriterionId = request.EvaluationCriterionId,
                Score = request.Score,
                Comment = request.Comment,
            };
            EvaluationScore createdEntity = await _repository.AddAsync(entity);
            return _mapper.Map<CreatedEvaluationScoreResponse>(createdEntity);
        }
    }
}
