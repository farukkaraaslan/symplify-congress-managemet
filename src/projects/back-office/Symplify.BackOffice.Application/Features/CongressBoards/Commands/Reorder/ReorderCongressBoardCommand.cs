using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressBoards.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressBoards.Commands.Reorder;

public sealed class ReorderCongressBoardCommand : IRequest<ReorderedCongressBoardResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressId { get; set; }

    public ICollection<ReorderCongressBoardItemDto> Items { get; set; } = new List<ReorderCongressBoardItemDto>();

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetCongressBoards";

    public string[] Roles => new[] { CongressBoardsOperationClaims.Admin, CongressBoardsOperationClaims.Write, CongressBoardsOperationClaims.Update };

    public sealed class ReorderCongressBoardCommandHandler : IRequestHandler<ReorderCongressBoardCommand, ReorderedCongressBoardResponse>
    {
        private readonly ICongressBoardRepository _repository;

        public ReorderCongressBoardCommandHandler(ICongressBoardRepository repository)
        {
            _repository = repository;
        }

        public async Task<ReorderedCongressBoardResponse> Handle(
            ReorderCongressBoardCommand request,
            CancellationToken cancellationToken)
        {
            if (request.CongressId == Guid.Empty)
                throw new BusinessException(CongressBoardsMessages.CongressRequired);

            List<ReorderCongressBoardItemDto> requestedItems = request.Items
                .Where(item => item.Id != Guid.Empty)
                .GroupBy(item => item.Id)
                .Select(group => group.Last())
                .OrderBy(item => item.Order <= 0 ? int.MaxValue : item.Order)
                .ToList();

            if (requestedItems.Count == 0)
                throw new BusinessException(CongressBoardsMessages.ReorderRequired);

            List<CongressBoard> allVisibleBoards = _repository
                .Query()
                .ToList()
                .Where(board => board.CongressId == request.CongressId && !IsDeleted(board))
                .OrderBy(board => board.Order <= 0 ? int.MaxValue : board.Order)
                .ThenBy(board => board.Id)
                .ToList();

            Dictionary<Guid, CongressBoard> boardById = allVisibleBoards.ToDictionary(board => board.Id);

            if (requestedItems.Any(item => !boardById.ContainsKey(item.Id)))
                throw new BusinessException(CongressBoardsMessages.InvalidReorderList);

            HashSet<Guid> requestedIds = requestedItems.Select(item => item.Id).ToHashSet();
            List<CongressBoard> reorderedBoards = requestedItems.Select(item => boardById[item.Id]).ToList();
            List<CongressBoard> remainingBoards = allVisibleBoards.Where(board => !requestedIds.Contains(board.Id)).ToList();

            int insertOrder = requestedItems
                .Where(item => item.Order > 0)
                .Select(item => item.Order)
                .DefaultIfEmpty(1)
                .Min();

            int insertIndex = Math.Clamp(insertOrder - 1, 0, remainingBoards.Count);
            remainingBoards.InsertRange(insertIndex, reorderedBoards);

            int updatedCount = await PersistNormalizedOrdersAsync(remainingBoards, cancellationToken);

            return new ReorderedCongressBoardResponse { UpdatedCount = updatedCount };
        }

        private async Task<int> PersistNormalizedOrdersAsync(
            IReadOnlyList<CongressBoard> boards,
            CancellationToken cancellationToken)
        {
            int updatedCount = 0;

            for (int index = 0; index < boards.Count; index++)
            {
                int normalizedOrder = index + 1;

                if (boards[index].Order == normalizedOrder)
                    continue;

                boards[index].Order = normalizedOrder;
                await _repository.UpdateAsync(boards[index]);
                updatedCount++;
            }

            return updatedCount;
        }

        private static bool IsDeleted(object entity)
            => LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate") is not null;
    }
}

public sealed class ReorderCongressBoardItemDto
{
    public Guid Id { get; set; }

    public int Order { get; set; }
}
