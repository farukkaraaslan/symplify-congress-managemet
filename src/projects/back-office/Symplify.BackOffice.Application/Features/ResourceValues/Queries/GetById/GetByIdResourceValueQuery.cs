using AutoMapper;
using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.ResourceValues.Constants;
using Symplify.BackOffice.Application.Features.ResourceValues.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Localization;
namespace Symplify.BackOffice.Application.Features.ResourceValues.Queries.GetById;
public class GetByIdResourceValueQuery : IRequest<GetByIdResourceValueResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { ResourceValuesOperationClaims.Admin, ResourceValuesOperationClaims.Read };
    public class GetByIdResourceValueQueryHandler : IRequestHandler<GetByIdResourceValueQuery, GetByIdResourceValueResponse>
    {
        private readonly IResourceValueRepository _repository; private readonly IMapper _mapper; private readonly ResourceValueBusinessRules _rules;
        public GetByIdResourceValueQueryHandler(IResourceValueRepository repository, IMapper mapper, ResourceValueBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<GetByIdResourceValueResponse> Handle(GetByIdResourceValueQuery request, CancellationToken cancellationToken)
        {
            ResourceValue? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.ResourceValueShouldExistWhenSelected(entity);
            return _mapper.Map<GetByIdResourceValueResponse>(entity);
        }
    }
}
