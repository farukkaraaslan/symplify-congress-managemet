using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressEvaluationCriteria.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressEvaluationCriteria.Commands.Create;
public class CreateCongressEvaluationCriterionCommand : IRequest<CreatedCongressEvaluationCriterionResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressId { get; set; }
    public Guid EvaluationCriterionId { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressEvaluationCriteria";
    public string[] Roles => new[] { CongressEvaluationCriteriaOperationClaims.Admin, CongressEvaluationCriteriaOperationClaims.Write, CongressEvaluationCriteriaOperationClaims.Add };
    public class CreateCongressEvaluationCriterionCommandHandler : IRequestHandler<CreateCongressEvaluationCriterionCommand, CreatedCongressEvaluationCriterionResponse>
    {
        private readonly ICongressEvaluationCriterionRepository _repository;
        private readonly IMapper _mapper;
        public CreateCongressEvaluationCriterionCommandHandler(ICongressEvaluationCriterionRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<CreatedCongressEvaluationCriterionResponse> Handle(CreateCongressEvaluationCriterionCommand request, CancellationToken cancellationToken)
        {
            CongressEvaluationCriterion entity = new()
            {
                Id = Guid.NewGuid(),
                CongressId = request.CongressId,
                EvaluationCriterionId = request.EvaluationCriterionId,
                Order = request.Order,
                IsActive = request.IsActive,
            };
            CongressEvaluationCriterion createdEntity = await _repository.AddAsync(entity);
            return _mapper.Map<CreatedCongressEvaluationCriterionResponse>(createdEntity);
        }
    }
}
