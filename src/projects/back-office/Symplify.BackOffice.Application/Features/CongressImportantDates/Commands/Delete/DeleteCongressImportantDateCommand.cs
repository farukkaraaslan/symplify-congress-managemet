using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressImportantDates.Constants;
using Symplify.BackOffice.Application.Features.CongressImportantDates.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressImportantDates.Commands.Delete;
public class DeleteCongressImportantDateCommand : IRequest<DeletedCongressImportantDateResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressImportantDates";
    public string[] Roles => new[] { CongressImportantDatesOperationClaims.Admin, CongressImportantDatesOperationClaims.Write, CongressImportantDatesOperationClaims.Delete };
    public class DeleteCongressImportantDateCommandHandler : IRequestHandler<DeleteCongressImportantDateCommand, DeletedCongressImportantDateResponse>
    {
        private readonly ICongressImportantDateRepository _repository; private readonly IMapper _mapper; private readonly CongressImportantDateBusinessRules _rules;
        public DeleteCongressImportantDateCommandHandler(ICongressImportantDateRepository repository, IMapper mapper, CongressImportantDateBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedCongressImportantDateResponse> Handle(DeleteCongressImportantDateCommand request, CancellationToken cancellationToken)
        {
            CongressImportantDate? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.CongressImportantDateShouldExistWhenSelected(entity);
            CongressImportantDate deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedCongressImportantDateResponse>(deletedEntity);
        }
    }
}
