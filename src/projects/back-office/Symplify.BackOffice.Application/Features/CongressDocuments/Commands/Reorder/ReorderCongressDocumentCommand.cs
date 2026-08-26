using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressDocuments.Constants;
using Symplify.BackOffice.Application.Features.CongressDocuments.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressDocuments.Commands.Reorder;

public sealed class ReorderCongressDocumentCommand : IRequest<ReorderedCongressDocumentResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressId { get; set; }
    public ICollection<ReorderCongressDocumentItemDto> Items { get; set; } = new List<ReorderCongressDocumentItemDto>();
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressDocuments";
    public string[] Roles => new[] { CongressDocumentsOperationClaims.Admin, CongressDocumentsOperationClaims.Write, CongressDocumentsOperationClaims.Update };

    public sealed class ReorderCongressDocumentCommandHandler : IRequestHandler<ReorderCongressDocumentCommand, ReorderedCongressDocumentResponse>
    {
        private readonly ICongressDocumentRepository _repository;
        private readonly CongressDocumentBusinessRules _rules;
        public ReorderCongressDocumentCommandHandler(ICongressDocumentRepository repository, CongressDocumentBusinessRules rules) { _repository = repository; _rules = rules; }
        public async Task<ReorderedCongressDocumentResponse> Handle(ReorderCongressDocumentCommand request, CancellationToken cancellationToken)
        {
            await _rules.CongressShouldExist(request.CongressId, cancellationToken);
            List<ReorderCongressDocumentItemDto> requestedItems = request.Items.Where(item => item.Id != Guid.Empty).GroupBy(item => item.Id).Select(group => group.Last()).OrderBy(item => item.Order <= 0 ? int.MaxValue : item.Order).ToList();
            IReadOnlyCollection<Guid> requestedIdsForRule = requestedItems.Select(item => item.Id).ToList();
            await _rules.ReorderItemsShouldBeValid(requestedIdsForRule);
            List<CongressDocument> allVisibleEntities = _repository.Query().ToList().Where(entity => entity.CongressId == request.CongressId && !IsDeleted(entity)).OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order).ThenBy(entity => entity.Id).ToList();
            Dictionary<Guid, CongressDocument> entityById = allVisibleEntities.ToDictionary(entity => entity.Id);
            await _rules.ReorderItemsShouldBelongToCongress(requestedIdsForRule, entityById);
            HashSet<Guid> requestedIds = requestedItems.Select(item => item.Id).ToHashSet();
            List<CongressDocument> reorderedEntities = requestedItems.Select(item => entityById[item.Id]).ToList();
            List<CongressDocument> remainingEntities = allVisibleEntities.Where(entity => !requestedIds.Contains(entity.Id)).ToList();
            int insertOrder = requestedItems.Where(item => item.Order > 0).Select(item => item.Order).DefaultIfEmpty(1).Min();
            int insertIndex = Math.Clamp(insertOrder - 1, 0, remainingEntities.Count);
            remainingEntities.InsertRange(insertIndex, reorderedEntities);
            int updatedCount = await PersistNormalizedOrdersAsync(remainingEntities, cancellationToken);
            return new ReorderedCongressDocumentResponse { UpdatedCount = updatedCount };
        }
        private async Task<int> PersistNormalizedOrdersAsync(IReadOnlyList<CongressDocument> entities, CancellationToken cancellationToken)
        { int updatedCount = 0; for (int index = 0; index < entities.Count; index++) { int normalizedOrder = index + 1; if (entities[index].Order == normalizedOrder) continue; entities[index].Order = normalizedOrder; await _repository.UpdateAsync(entities[index]); updatedCount++; } return updatedCount; }
        private static bool IsDeleted(object entity) => LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate") is not null;
    }
}

public sealed class ReorderCongressDocumentItemDto { public Guid Id { get; set; } public int Order { get; set; } }
