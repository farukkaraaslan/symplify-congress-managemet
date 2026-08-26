using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Features.Submissions.Constants;
using Symplify.BackOffice.Application.Features.Submissions.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Application.Features.Submissions.Commands.Delete;

public sealed class DeleteSubmissionCommand : IRequest<DeletedSubmissionResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }

    public Guid? CongressId { get; set; }

    public Guid? RequestedByUserId { get; set; }

    public bool RequestedByCanManageAllSubmissions { get; set; }

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetSubmissions";

    public string[] Roles => new[] { SubmissionsOperationClaims.Admin, SubmissionsOperationClaims.Write, SubmissionsOperationClaims.Delete };

    public sealed class DeleteSubmissionCommandHandler : IRequestHandler<DeleteSubmissionCommand, DeletedSubmissionResponse>
    {
        private readonly ISubmissionRepository _repository;
        private readonly IMapper _mapper;
        private readonly SubmissionBusinessRules _rules;

        public DeleteSubmissionCommandHandler(
            ISubmissionRepository repository,
            IMapper mapper,
            SubmissionBusinessRules rules)
        {
            _repository = repository;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<DeletedSubmissionResponse> Handle(DeleteSubmissionCommand request, CancellationToken cancellationToken)
        {
            Submission? entity = await _repository
                .Query()
                .Include(submission => submission.TransactionStatus)
                .FirstOrDefaultAsync(submission =>
                        submission.Id == request.Id &&
                        (!request.CongressId.HasValue || submission.CongressId == request.CongressId.Value),
                    cancellationToken);

            await _rules.SubmissionShouldExistWhenSelected(entity);
            entity = entity!;
            await _rules.SubmissionShouldBeAccessibleForUser(entity, request.RequestedByUserId, request.RequestedByCanManageAllSubmissions);
            await _rules.SubmissionShouldBeEditable(entity, request.RequestedByCanManageAllSubmissions);

            Submission deletedEntity = await _repository.DeleteAsync(entity);
            return _mapper.Map<DeletedSubmissionResponse>(deletedEntity);
        }
    }
}
