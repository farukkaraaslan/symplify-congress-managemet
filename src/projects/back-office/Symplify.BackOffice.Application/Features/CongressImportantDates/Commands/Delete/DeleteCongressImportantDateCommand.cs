using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressImportantDates.Constants;
using Symplify.BackOffice.Application.Features.CongressImportantDates.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressImportantDates.Commands.Delete;

public class DeleteCongressImportantDateCommand
    : IRequest<DeletedCongressImportantDateResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetCongressImportantDates";

    public string[] Roles => new[]
    {
        CongressImportantDatesOperationClaims.Admin,
        CongressImportantDatesOperationClaims.Write,
        CongressImportantDatesOperationClaims.Delete
    };

    public class DeleteCongressImportantDateCommandHandler
        : IRequestHandler<DeleteCongressImportantDateCommand, DeletedCongressImportantDateResponse>
    {
        private readonly ICongressImportantDateRepository _repository;
        private readonly IMapper _mapper;
        private readonly CongressImportantDateBusinessRules _rules;

        public DeleteCongressImportantDateCommandHandler(
            ICongressImportantDateRepository repository,
            IMapper mapper,
            CongressImportantDateBusinessRules rules)
        {
            _repository = repository;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<DeletedCongressImportantDateResponse> Handle(
            DeleteCongressImportantDateCommand request,
            CancellationToken cancellationToken)
        {
            CongressImportantDate? entity = await _repository.GetAsync(
                predicate: item => item.Id.Equals(request.Id));

            await _rules.CongressImportantDateShouldExistWhenSelected(entity);

            Guid congressId = entity!.CongressId;

            NormalizeEntityDateTimesToUtc(entity);

            CongressImportantDate deletedEntity = await _repository.DeleteAsync(entity);

            await NormalizeVisibleOrdersAsync(
                congressId,
                request.Id,
                cancellationToken);

            return _mapper.Map<DeletedCongressImportantDateResponse>(deletedEntity);
        }

        private async Task NormalizeVisibleOrdersAsync(
            Guid congressId,
            Guid deletedEntityId,
            CancellationToken cancellationToken)
        {
            List<CongressImportantDate> entities = _repository
                .Query()
                .ToList()
                .Where(entity =>
                    entity.CongressId == congressId &&
                    !IsDeleted(entity) &&
                    entity.Id != deletedEntityId)
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.StartDate)
                .ThenBy(entity => entity.EndDate)
                .ThenBy(entity => entity.Id)
                .ToList();

            for (int index = 0; index < entities.Count; index++)
            {
                int normalizedOrder = index + 1;

                NormalizeEntityDateTimesToUtc(entities[index]);

                if (entities[index].Order == normalizedOrder)
                    continue;

                entities[index].Order = normalizedOrder;

                await _repository.UpdateAsync(entities[index]);
            }
        }

        private static void NormalizeEntityDateTimesToUtc(CongressImportantDate entity)
        {
            entity.StartDate = ConvertToUtc(entity.StartDate);
            entity.EndDate = ConvertToUtc(entity.EndDate);
        }

        private static DateTime ConvertToUtc(DateTime value)
        {
            if (value == default)
                return value;

            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime(),
                _ => value
            };
        }

        private static bool IsDeleted(object entity)
        {
            object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(
                entity,
                "DeletedDate");

            return deletedDate is not null;
        }
    }
}