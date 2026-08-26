using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
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
        private readonly ICongressBoardRepository _repository;
        private readonly ICongressBoardMemberRepository _memberRepository;
        private readonly IMapper _mapper;
        private readonly CongressBoardBusinessRules _rules;

        public DeleteCongressBoardCommandHandler(
            ICongressBoardRepository repository,
            ICongressBoardMemberRepository memberRepository,
            IMapper mapper,
            CongressBoardBusinessRules rules)
        {
            _repository = repository;
            _memberRepository = memberRepository;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<DeletedCongressBoardResponse> Handle(
            DeleteCongressBoardCommand request,
            CancellationToken cancellationToken)
        {
            CongressBoard? entity = await _repository.GetAsync(predicate: board => board.Id.Equals(request.Id));
            await _rules.CongressBoardShouldExistWhenSelected(entity);

            bool hasMembers = _memberRepository
                .Query()
                .ToList()
                .Any(member => member.CongressBoardId == request.Id && !IsDeleted(member));

            if (hasMembers)
                throw new BusinessException(CongressBoardsMessages.BoardHasMembers);

            Guid congressId = entity!.CongressId;
            CongressBoard deletedEntity = await _repository.DeleteAsync(entity);

            await NormalizeOrdersAsync(congressId, cancellationToken);

            return _mapper.Map<DeletedCongressBoardResponse>(deletedEntity);
        }

        private async Task NormalizeOrdersAsync(Guid congressId, CancellationToken cancellationToken)
        {
            List<CongressBoard> boards = _repository
                .Query()
                .ToList()
                .Where(board => board.CongressId == congressId && !IsDeleted(board))
                .OrderBy(board => board.Order <= 0 ? int.MaxValue : board.Order)
                .ThenBy(board => board.Id)
                .ToList();

            for (int index = 0; index < boards.Count; index++)
            {
                int normalizedOrder = index + 1;

                if (boards[index].Order == normalizedOrder)
                    continue;

                boards[index].Order = normalizedOrder;
                await _repository.UpdateAsync(boards[index]);
            }
        }

        private static bool IsDeleted(object entity)
            => LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate") is not null;
    }
}
