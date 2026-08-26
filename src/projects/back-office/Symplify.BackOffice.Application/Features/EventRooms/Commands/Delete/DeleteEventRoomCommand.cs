using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.EventRooms.Constants;
using Symplify.BackOffice.Application.Features.EventRooms.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Application.Features.EventRooms.Commands.Delete;

public class DeleteEventRoomCommand : IRequest<DeletedEventRoomResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetEventRooms";

    public string[] Roles => new[] { EventRoomsOperationClaims.Admin, EventRoomsOperationClaims.Write, EventRoomsOperationClaims.Delete };

    public class DeleteEventRoomCommandHandler : IRequestHandler<DeleteEventRoomCommand, DeletedEventRoomResponse>
    {
        private readonly IEventRoomRepository _repository;
        private readonly IMapper _mapper;
        private readonly EventRoomBusinessRules _rules;

        public DeleteEventRoomCommandHandler(
            IEventRoomRepository repository,
            IMapper mapper,
            EventRoomBusinessRules rules)
        {
            _repository = repository;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<DeletedEventRoomResponse> Handle(DeleteEventRoomCommand request, CancellationToken cancellationToken)
        {
            EventRoom? entity = await _repository.GetAsync(predicate: item => item.Id.Equals(request.Id));
            await _rules.EventRoomShouldExistWhenSelected(entity);

            EventRoom deletedEntity = await _repository.DeleteAsync(entity!);
            await NormalizeVisibleOrdersAsync(request.Id, cancellationToken);

            return _mapper.Map<DeletedEventRoomResponse>(deletedEntity);
        }

        private async Task NormalizeVisibleOrdersAsync(Guid deletedEntityId, CancellationToken cancellationToken)
        {
            List<EventRoom> entities = _repository.Query()
                .ToList()
                .Where(entity => !IsDeleted(entity) && entity.Id != deletedEntityId)
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
