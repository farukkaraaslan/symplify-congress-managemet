using AutoMapper;
using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.SubmissionHistories.Constants;
using Symplify.BackOffice.Application.Features.SubmissionHistories.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.SubmissionHistories.Queries.GetById;
public class GetByIdSubmissionHistoryQuery : IRequest<GetByIdSubmissionHistoryResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { SubmissionHistoriesOperationClaims.Admin, SubmissionHistoriesOperationClaims.Read };
    public class GetByIdSubmissionHistoryQueryHandler : IRequestHandler<GetByIdSubmissionHistoryQuery, GetByIdSubmissionHistoryResponse>
    {
        private readonly ISubmissionHistoryRepository _repository; private readonly IMapper _mapper; private readonly SubmissionHistoryBusinessRules _rules;
        public GetByIdSubmissionHistoryQueryHandler(ISubmissionHistoryRepository repository, IMapper mapper, SubmissionHistoryBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<GetByIdSubmissionHistoryResponse> Handle(GetByIdSubmissionHistoryQuery request, CancellationToken cancellationToken)
        {
            SubmissionHistory? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.SubmissionHistoryShouldExistWhenSelected(entity);
            return _mapper.Map<GetByIdSubmissionHistoryResponse>(entity);
        }
    }
}
