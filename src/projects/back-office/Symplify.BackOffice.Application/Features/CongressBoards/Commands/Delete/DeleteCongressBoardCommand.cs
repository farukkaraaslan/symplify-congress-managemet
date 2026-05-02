using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressBoards.Constants;
using Symplify.BackOffice.Application.Features.CongressBoards.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressBoards.Commands.Delete;
public class DeleteCongressBoardCommand : IRequest<DeletedCongressBoardResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressBoards";
    public string[] Roles => new[] { CongressBoardsOperationClaims.Admin, CongressBoardsOperationClaims.Write, CongressBoardsOperationClaims.Delete };
    public class DeleteCongressBoardCommandHandler : IRequestHandler<DeleteCongressBoardCommand, DeletedCongressBoardResponse>
    {
        private readonly ICongressBoardRepository _repository; private readonly IMapper _mapper; private readonly CongressBoardBusinessRules _rules;
        public DeleteCongressBoardCommandHandler(ICongressBoardRepository repository, IMapper mapper, CongressBoardBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedCongressBoardResponse> Handle(DeleteCongressBoardCommand request, CancellationToken cancellationToken)
        {
            CongressBoard? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.CongressBoardShouldExistWhenSelected(entity);
            CongressBoard deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedCongressBoardResponse>(deletedEntity);
        }
    }
}
