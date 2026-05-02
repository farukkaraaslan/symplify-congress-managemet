using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressSections.Constants;
using Symplify.BackOffice.Application.Features.CongressSections.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressSections.Commands.Delete;
public class DeleteCongressSectionCommand : IRequest<DeletedCongressSectionResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressSections";
    public string[] Roles => new[] { CongressSectionsOperationClaims.Admin, CongressSectionsOperationClaims.Write, CongressSectionsOperationClaims.Delete };
    public class DeleteCongressSectionCommandHandler : IRequestHandler<DeleteCongressSectionCommand, DeletedCongressSectionResponse>
    {
        private readonly ICongressSectionRepository _repository; private readonly IMapper _mapper; private readonly CongressSectionBusinessRules _rules;
        public DeleteCongressSectionCommandHandler(ICongressSectionRepository repository, IMapper mapper, CongressSectionBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedCongressSectionResponse> Handle(DeleteCongressSectionCommand request, CancellationToken cancellationToken)
        {
            CongressSection? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.CongressSectionShouldExistWhenSelected(entity);
            CongressSection deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedCongressSectionResponse>(deletedEntity);
        }
    }
}
