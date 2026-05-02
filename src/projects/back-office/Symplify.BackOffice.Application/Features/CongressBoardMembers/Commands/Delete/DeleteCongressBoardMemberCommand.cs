using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Constants;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands.Delete;
public class DeleteCongressBoardMemberCommand : IRequest<DeletedCongressBoardMemberResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressBoardMembers";
    public string[] Roles => new[] { CongressBoardMembersOperationClaims.Admin, CongressBoardMembersOperationClaims.Write, CongressBoardMembersOperationClaims.Delete };
    public class DeleteCongressBoardMemberCommandHandler : IRequestHandler<DeleteCongressBoardMemberCommand, DeletedCongressBoardMemberResponse>
    {
        private readonly ICongressBoardMemberRepository _repository; private readonly IMapper _mapper; private readonly CongressBoardMemberBusinessRules _rules;
        public DeleteCongressBoardMemberCommandHandler(ICongressBoardMemberRepository repository, IMapper mapper, CongressBoardMemberBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedCongressBoardMemberResponse> Handle(DeleteCongressBoardMemberCommand request, CancellationToken cancellationToken)
        {
            CongressBoardMember? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.CongressBoardMemberShouldExistWhenSelected(entity);
            CongressBoardMember deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedCongressBoardMemberResponse>(deletedEntity);
        }
    }
}
