using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.SubmissionHistories.Constants;
using Symplify.BackOffice.Application.Features.SubmissionHistories.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.SubmissionHistories.Commands.Update;
public class UpdateSubmissionHistoryCommand : IRequest<UpdatedSubmissionHistoryResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public int? FromStatusId { get; set; }
    public int? ToStatusId { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public string? Note { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetSubmissionHistories";
    public string[] Roles => new[] { SubmissionHistoriesOperationClaims.Admin, SubmissionHistoriesOperationClaims.Write, SubmissionHistoriesOperationClaims.Update };
    public class UpdateSubmissionHistoryCommandHandler : IRequestHandler<UpdateSubmissionHistoryCommand, UpdatedSubmissionHistoryResponse>
    {
        private readonly ISubmissionHistoryRepository _repository; private readonly IMapper _mapper; private readonly SubmissionHistoryBusinessRules _rules;
        public UpdateSubmissionHistoryCommandHandler(ISubmissionHistoryRepository repository, IMapper mapper, SubmissionHistoryBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<UpdatedSubmissionHistoryResponse> Handle(UpdateSubmissionHistoryCommand request, CancellationToken cancellationToken)
        {
            SubmissionHistory? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.SubmissionHistoryShouldExistWhenSelected(entity);
            entity!.SubmissionId = request.SubmissionId;
            entity!.FromStatusId = request.FromStatusId;
            entity!.ToStatusId = request.ToStatusId;
            entity!.PerformedByUserId = request.PerformedByUserId;
            entity!.Note = request.Note;
            SubmissionHistory updatedEntity = await _repository.UpdateAsync(entity!);
            return _mapper.Map<UpdatedSubmissionHistoryResponse>(updatedEntity);
        }
    }
}
