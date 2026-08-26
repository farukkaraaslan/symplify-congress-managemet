using AutoMapper;
using Symplify.BackOffice.Application.Common.Localization;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressAnnouncements.Constants;
using Symplify.BackOffice.Application.Features.CongressAnnouncements.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressAnnouncements.Commands.Delete;

public class DeleteCongressAnnouncementCommand : IRequest<DeletedCongressAnnouncementResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressAnnouncements";
    public string[] Roles => new[] { CongressAnnouncementsOperationClaims.Admin, CongressAnnouncementsOperationClaims.Write, CongressAnnouncementsOperationClaims.Delete };

    public class Handler : IRequestHandler<DeleteCongressAnnouncementCommand, DeletedCongressAnnouncementResponse>
    {
        private readonly ICongressAnnouncementRepository _repository;
        private readonly IMapper _mapper;
        private readonly CongressAnnouncementBusinessRules _rules;

        public Handler(ICongressAnnouncementRepository repository, IMapper mapper, CongressAnnouncementBusinessRules rules)
        {
            _repository = repository;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<DeletedCongressAnnouncementResponse> Handle(DeleteCongressAnnouncementCommand request, CancellationToken cancellationToken)
        {
            CongressAnnouncement? entity = await _repository.GetAsync(predicate: item => item.Id == request.Id);
            await _rules.AnnouncementShouldExistWhenSelected(entity);

            Guid congressId = entity!.CongressId;
            CongressAnnouncement deletedEntity = await _repository.DeleteAsync(entity);

            await NormalizeVisibleOrdersAsync(congressId, deletedEntity.Id, cancellationToken);

            return _mapper.Map<DeletedCongressAnnouncementResponse>(deletedEntity);
        }

        private async Task NormalizeVisibleOrdersAsync(
            Guid congressId,
            Guid deletedEntityId,
            CancellationToken cancellationToken)
        {
            List<CongressAnnouncement> entities = _repository.Query()
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
