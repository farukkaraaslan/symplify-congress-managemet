using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.Submissions.Constants;
using Symplify.BackOffice.Application.Features.Submissions.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.Submissions.Commands.Update;
public class UpdateSubmissionCommand : IRequest<UpdatedSubmissionResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public Guid? SubmissionTypeId { get; set; }
    public Guid? TopicId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public int? PaymentStatusId { get; set; }
    public int? TransactionStatusId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Abstract { get; set; }
    public string? Keywords { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetSubmissions";
    public string[] Roles => new[] { SubmissionsOperationClaims.Admin, SubmissionsOperationClaims.Write, SubmissionsOperationClaims.Update };
    public class UpdateSubmissionCommandHandler : IRequestHandler<UpdateSubmissionCommand, UpdatedSubmissionResponse>
    {
        private readonly ISubmissionRepository _repository; private readonly IMapper _mapper; private readonly SubmissionBusinessRules _rules;
        public UpdateSubmissionCommandHandler(ISubmissionRepository repository, IMapper mapper, SubmissionBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<UpdatedSubmissionResponse> Handle(UpdateSubmissionCommand request, CancellationToken cancellationToken)
        {
            Submission? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.SubmissionShouldExistWhenSelected(entity);
            entity!.CongressId = request.CongressId;
            entity!.SubmissionTypeId = request.SubmissionTypeId;
            entity!.TopicId = request.TopicId;
            entity!.CreatedByUserId = request.CreatedByUserId;
            entity!.PaymentStatusId = request.PaymentStatusId;
            entity!.TransactionStatusId = request.TransactionStatusId;
            entity!.Title = request.Title;
            entity!.Abstract = request.Abstract;
            entity!.Keywords = request.Keywords;
            Submission updatedEntity = await _repository.UpdateAsync(entity!);
            return _mapper.Map<UpdatedSubmissionResponse>(updatedEntity);
        }
    }
}
