using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.SubmissionHistories.Constants;
using Symplify.BackOffice.Application.Features.SubmissionHistories.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.SubmissionHistories.Commands.Delete;
public class DeleteSubmissionHistoryCommand : IRequest<DeletedSubmissionHistoryResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetSubmissionHistories";
    public string[] Roles => new[] { SubmissionHistoriesOperationClaims.Admin, SubmissionHistoriesOperationClaims.Write, SubmissionHistoriesOperationClaims.Delete };
    public class DeleteSubmissionHistoryCommandHandler : IRequestHandler<DeleteSubmissionHistoryCommand, DeletedSubmissionHistoryResponse>
    {
        private readonly ISubmissionHistoryRepository _repository; private readonly IMapper _mapper; private readonly SubmissionHistoryBusinessRules _rules;
        public DeleteSubmissionHistoryCommandHandler(ISubmissionHistoryRepository repository, IMapper mapper, SubmissionHistoryBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedSubmissionHistoryResponse> Handle(DeleteSubmissionHistoryCommand request, CancellationToken cancellationToken)
        {
            SubmissionHistory? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.SubmissionHistoryShouldExistWhenSelected(entity);
            SubmissionHistory deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedSubmissionHistoryResponse>(deletedEntity);
        }
    }
}
