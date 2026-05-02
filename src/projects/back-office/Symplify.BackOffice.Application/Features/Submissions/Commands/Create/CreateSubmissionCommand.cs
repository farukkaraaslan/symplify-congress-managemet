using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.Submissions.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.Submissions.Commands.Create;
public class CreateSubmissionCommand : IRequest<CreatedSubmissionResponse>, ISecuredRequest, ICacheRemoverRequest
{
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
    public string[] Roles => new[] { SubmissionsOperationClaims.Admin, SubmissionsOperationClaims.Write, SubmissionsOperationClaims.Add };
    public class CreateSubmissionCommandHandler : IRequestHandler<CreateSubmissionCommand, CreatedSubmissionResponse>
    {
        private readonly ISubmissionRepository _repository;
        private readonly IMapper _mapper;
        public CreateSubmissionCommandHandler(ISubmissionRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<CreatedSubmissionResponse> Handle(CreateSubmissionCommand request, CancellationToken cancellationToken)
        {
            Submission entity = new()
            {
                Id = Guid.NewGuid(),
                CongressId = request.CongressId,
                SubmissionTypeId = request.SubmissionTypeId,
                TopicId = request.TopicId,
                CreatedByUserId = request.CreatedByUserId,
                PaymentStatusId = request.PaymentStatusId,
                TransactionStatusId = request.TransactionStatusId,
                Title = request.Title,
                Abstract = request.Abstract,
                Keywords = request.Keywords,
            };
            Submission createdEntity = await _repository.AddAsync(entity);
            return _mapper.Map<CreatedSubmissionResponse>(createdEntity);
        }
    }
}
