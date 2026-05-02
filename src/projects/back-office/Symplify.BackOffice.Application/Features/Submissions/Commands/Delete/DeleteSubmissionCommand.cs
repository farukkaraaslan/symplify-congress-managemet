using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.Submissions.Constants;
using Symplify.BackOffice.Application.Features.Submissions.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.Submissions.Commands.Delete;
public class DeleteSubmissionCommand : IRequest<DeletedSubmissionResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetSubmissions";
    public string[] Roles => new[] { SubmissionsOperationClaims.Admin, SubmissionsOperationClaims.Write, SubmissionsOperationClaims.Delete };
    public class DeleteSubmissionCommandHandler : IRequestHandler<DeleteSubmissionCommand, DeletedSubmissionResponse>
    {
        private readonly ISubmissionRepository _repository; private readonly IMapper _mapper; private readonly SubmissionBusinessRules _rules;
        public DeleteSubmissionCommandHandler(ISubmissionRepository repository, IMapper mapper, SubmissionBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedSubmissionResponse> Handle(DeleteSubmissionCommand request, CancellationToken cancellationToken)
        {
            Submission? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.SubmissionShouldExistWhenSelected(entity);
            Submission deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedSubmissionResponse>(deletedEntity);
        }
    }
}
