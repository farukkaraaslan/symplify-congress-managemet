using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.SubmissionHistories.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.SubmissionHistories.Commands.Create;
public class CreateSubmissionHistoryCommand : IRequest<CreatedSubmissionHistoryResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid SubmissionId { get; set; }
    public int? FromStatusId { get; set; }
    public int? ToStatusId { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public string? Note { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetSubmissionHistories";
    public string[] Roles => new[] { SubmissionHistoriesOperationClaims.Admin, SubmissionHistoriesOperationClaims.Write, SubmissionHistoriesOperationClaims.Add };
    public class CreateSubmissionHistoryCommandHandler : IRequestHandler<CreateSubmissionHistoryCommand, CreatedSubmissionHistoryResponse>
    {
        private readonly ISubmissionHistoryRepository _repository;
        private readonly IMapper _mapper;
        public CreateSubmissionHistoryCommandHandler(ISubmissionHistoryRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<CreatedSubmissionHistoryResponse> Handle(CreateSubmissionHistoryCommand request, CancellationToken cancellationToken)
        {
            SubmissionHistory entity = new()
            {
                Id = Guid.NewGuid(),
                SubmissionId = request.SubmissionId,
                FromStatusId = request.FromStatusId,
                ToStatusId = request.ToStatusId,
                PerformedByUserId = request.PerformedByUserId,
                Note = request.Note,
            };
            SubmissionHistory createdEntity = await _repository.AddAsync(entity);
            return _mapper.Map<CreatedSubmissionHistoryResponse>(createdEntity);
        }
    }
}
