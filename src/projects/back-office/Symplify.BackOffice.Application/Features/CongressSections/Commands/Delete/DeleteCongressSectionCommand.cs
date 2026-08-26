using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
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

    public class DeleteCongressSectionCommandHandler
        : IRequestHandler<DeleteCongressSectionCommand, DeletedCongressSectionResponse>
    {
        private readonly ICongressSectionRepository _repository;
        private readonly IMapper _mapper;
        private readonly CongressSectionBusinessRules _rules;

        public DeleteCongressSectionCommandHandler(
            ICongressSectionRepository repository,
            IMapper mapper,
            CongressSectionBusinessRules rules)
        {
            _repository = repository;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<DeletedCongressSectionResponse> Handle(
            DeleteCongressSectionCommand request,
            CancellationToken cancellationToken)
        {
            CongressSection? entity = await _repository.GetAsync(predicate: item => item.Id == request.Id);
            await _rules.CongressSectionShouldExistWhenSelected(entity);

            Guid congressId = entity!.CongressId;
            CongressSection deletedEntity = await _repository.DeleteAsync(entity);

            await NormalizeVisibleOrdersAsync(congressId, deletedEntity.Id, cancellationToken);

            return _mapper.Map<DeletedCongressSectionResponse>(deletedEntity);
        }

        private async Task NormalizeVisibleOrdersAsync(
            Guid congressId,
            Guid deletedEntityId,
            CancellationToken cancellationToken)
        {
            List<CongressSection> entities = _repository.Query()
                .ToList()
                .Where(entity =>
                    entity.CongressId == congressId &&
                    entity.Id != deletedEntityId &&
                    !IsDeleted(entity))
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.Id)
                .ToList();

            for (int index = 0; index < entities.Count; index++)
            {
                int normalizedOrder = index + 1;

                if (entities[index].Order == normalizedOrder)
                    continue;

                entities[index].Order = normalizedOrder;
                await _repository.UpdateAsync(entities[index]);
            }
        }

        private static bool IsDeleted(object entity)
        {
            object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
            return deletedDate is not null;
        }
    }
}
