using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Constants;
using Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Commands.Update;
public class UpdateCongressSubmissionTypeCommand : IRequest<UpdatedCongressSubmissionTypeResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public Guid SubmissionTypeId { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressSubmissionTypes";
    public string[] Roles => new[] { CongressSubmissionTypesOperationClaims.Admin, CongressSubmissionTypesOperationClaims.Write, CongressSubmissionTypesOperationClaims.Update };
    public class UpdateCongressSubmissionTypeCommandHandler : IRequestHandler<UpdateCongressSubmissionTypeCommand, UpdatedCongressSubmissionTypeResponse>
    {
        private readonly ICongressSubmissionTypeRepository _repository; private readonly IMapper _mapper; private readonly CongressSubmissionTypeBusinessRules _rules;
        public UpdateCongressSubmissionTypeCommandHandler(ICongressSubmissionTypeRepository repository, IMapper mapper, CongressSubmissionTypeBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<UpdatedCongressSubmissionTypeResponse> Handle(UpdateCongressSubmissionTypeCommand request, CancellationToken cancellationToken)
        {
            CongressSubmissionType? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.CongressSubmissionTypeShouldExistWhenSelected(entity);
            entity!.CongressId = request.CongressId;
            entity!.SubmissionTypeId = request.SubmissionTypeId;
            entity!.Order = request.Order;
            entity!.IsActive = request.IsActive;
            CongressSubmissionType updatedEntity = await _repository.UpdateAsync(entity!);
            return _mapper.Map<UpdatedCongressSubmissionTypeResponse>(updatedEntity);
        }
    }
}
