using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Constants;
using Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Commands.Delete;
public class DeleteCongressSubmissionTypeCommand : IRequest<DeletedCongressSubmissionTypeResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressSubmissionTypes";
    public string[] Roles => new[] { CongressSubmissionTypesOperationClaims.Admin, CongressSubmissionTypesOperationClaims.Write, CongressSubmissionTypesOperationClaims.Delete };
    public class DeleteCongressSubmissionTypeCommandHandler : IRequestHandler<DeleteCongressSubmissionTypeCommand, DeletedCongressSubmissionTypeResponse>
    {
        private readonly ICongressSubmissionTypeRepository _repository; private readonly IMapper _mapper; private readonly CongressSubmissionTypeBusinessRules _rules;
        public DeleteCongressSubmissionTypeCommandHandler(ICongressSubmissionTypeRepository repository, IMapper mapper, CongressSubmissionTypeBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedCongressSubmissionTypeResponse> Handle(DeleteCongressSubmissionTypeCommand request, CancellationToken cancellationToken)
        {
            CongressSubmissionType? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.CongressSubmissionTypeShouldExistWhenSelected(entity);
            CongressSubmissionType deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedCongressSubmissionTypeResponse>(deletedEntity);
        }
    }
}
