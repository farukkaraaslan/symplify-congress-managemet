using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands.Reorder;

public sealed class ReorderCongressBoardMemberCommand : IRequest<ReorderedCongressBoardMemberResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressId { get; set; }

    public ICollection<ReorderCongressBoardMemberItemDto> Items { get; set; } = new List<ReorderCongressBoardMemberItemDto>();

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetCongressBoardMembers";

    public string[] Roles => new[] { CongressBoardMembersOperationClaims.Admin, CongressBoardMembersOperationClaims.Write, CongressBoardMembersOperationClaims.Update };

    public sealed class ReorderCongressBoardMemberCommandHandler : IRequestHandler<ReorderCongressBoardMemberCommand, ReorderedCongressBoardMemberResponse>
    {
        private readonly ICongressBoardMemberRepository _repository;
        private readonly ICongressBoardRepository _boardRepository;

        public ReorderCongressBoardMemberCommandHandler(
            ICongressBoardMemberRepository repository,
            ICongressBoardRepository boardRepository)
        {
            _repository = repository;
            _boardRepository = boardRepository;
        }

        public async Task<ReorderedCongressBoardMemberResponse> Handle(
            ReorderCongressBoardMemberCommand request,
            CancellationToken cancellationToken)
        {
            if (request.CongressId == Guid.Empty)
                throw new BusinessException(CongressBoardMembersMessages.CongressRequired);

            List<ReorderCongressBoardMemberItemDto> requestedItems = request.Items
                .Where(item => item.Id != Guid.Empty)
                .GroupBy(item => item.Id)
                .Select(group => group.Last())
                .OrderBy(item => item.Order <= 0 ? int.MaxValue : item.Order)
                .ToList();

            if (requestedItems.Count == 0)
                throw new BusinessException(CongressBoardMembersMessages.ReorderRequired);

            HashSet<Guid> requestedIds = requestedItems.Select(item => item.Id).ToHashSet();
            List<CongressBoardMember> requestedMembers = _repository
                .Query()
                .ToList()
                .Where(member => requestedIds.Contains(member.Id) && !IsDeleted(member))
                .ToList();

            if (requestedMembers.Count != requestedIds.Count)
                throw new BusinessException(CongressBoardMembersMessages.InvalidReorderList);

            HashSet<Guid> boardIds = requestedMembers.Select(member => member.CongressBoardId).ToHashSet();

            if (boardIds.Count != 1)
                throw new BusinessException(CongressBoardMembersMessages.ReorderSingleBoardRequired);

            Guid boardId = boardIds.Single();
            CongressBoard? board = _boardRepository
                .Query()
                .ToList()
                .FirstOrDefault(item => item.Id == boardId && !IsDeleted(item));

            if (board is null || board.CongressId != request.CongressId)
                throw new BusinessException(CongressBoardMembersMessages.InvalidReorderList);

            List<CongressBoardMember> allVisibleMembers = _repository
                .Query()
                .ToList()
                .Where(member => member.CongressBoardId == boardId && !IsDeleted(member))
                .OrderBy(member => member.Order <= 0 ? int.MaxValue : member.Order)
                .ThenBy(member => member.Id)
                .ToList();

            Dictionary<Guid, CongressBoardMember> memberById = allVisibleMembers.ToDictionary(member => member.Id);

            if (requestedItems.Any(item => !memberById.ContainsKey(item.Id)))
                throw new BusinessException(CongressBoardMembersMessages.InvalidReorderList);

            List<CongressBoardMember> reorderedMembers = requestedItems.Select(item => memberById[item.Id]).ToList();
            List<CongressBoardMember> remainingMembers = allVisibleMembers.Where(member => !requestedIds.Contains(member.Id)).ToList();

            int insertOrder = requestedItems
                .Where(item => item.Order > 0)
                .Select(item => item.Order)
                .DefaultIfEmpty(1)
                .Min();

            int insertIndex = Math.Clamp(insertOrder - 1, 0, remainingMembers.Count);
            remainingMembers.InsertRange(insertIndex, reorderedMembers);

            int updatedCount = await PersistNormalizedOrdersAsync(remainingMembers, cancellationToken);

            return new ReorderedCongressBoardMemberResponse { UpdatedCount = updatedCount };
        }

        private async Task<int> PersistNormalizedOrdersAsync(
            IReadOnlyList<CongressBoardMember> members,
            CancellationToken cancellationToken)
        {
            int updatedCount = 0;

            for (int index = 0; index < members.Count; index++)
            {
                int normalizedOrder = index + 1;

                if (members[index].Order == normalizedOrder)
                    continue;

                members[index].Order = normalizedOrder;
                await _repository.UpdateAsync(members[index]);
                updatedCount++;
            }

            return updatedCount;
        }

        private static bool IsDeleted(object entity)
            => LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate") is not null;
    }
}

public sealed class ReorderCongressBoardMemberItemDto
{
    public Guid Id { get; set; }

    public int Order { get; set; }
}
