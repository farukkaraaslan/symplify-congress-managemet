using AutoMapper;
using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Constants;
using Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Queries.GetById;
public class GetByIdCongressSubmissionTypeQuery : IRequest<GetByIdCongressSubmissionTypeResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { CongressSubmissionTypesOperationClaims.Admin, CongressSubmissionTypesOperationClaims.Read };
    public class GetByIdCongressSubmissionTypeQueryHandler : IRequestHandler<GetByIdCongressSubmissionTypeQuery, GetByIdCongressSubmissionTypeResponse>
    {
        private readonly ICongressSubmissionTypeRepository _repository; private readonly IMapper _mapper; private readonly CongressSubmissionTypeBusinessRules _rules;
        public GetByIdCongressSubmissionTypeQueryHandler(ICongressSubmissionTypeRepository repository, IMapper mapper, CongressSubmissionTypeBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<GetByIdCongressSubmissionTypeResponse> Handle(GetByIdCongressSubmissionTypeQuery request, CancellationToken cancellationToken)
        {
            CongressSubmissionType? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.CongressSubmissionTypeShouldExistWhenSelected(entity);
            return _mapper.Map<GetByIdCongressSubmissionTypeResponse>(entity);
        }
    }
}
