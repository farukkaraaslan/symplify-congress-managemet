using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.Titles.Constants;
using Symplify.BackOffice.Application.Features.Titles.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Application.Features.Titles.Commands.Delete;

public class DeleteTitleCommand : IRequest<DeletedTitleResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetTitles";

    public string[] Roles => new[] { TitlesOperationClaims.Admin, TitlesOperationClaims.Write, TitlesOperationClaims.Delete };

    public class DeleteTitleCommandHandler : IRequestHandler<DeleteTitleCommand, DeletedTitleResponse>
    {
        private readonly ITitleRepository _repository;
        private readonly IMapper _mapper;
        private readonly TitleBusinessRules _rules;

        public DeleteTitleCommandHandler(
            ITitleRepository repository,
            IMapper mapper,
            TitleBusinessRules rules)
        {
            _repository = repository;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<DeletedTitleResponse> Handle(DeleteTitleCommand request, CancellationToken cancellationToken)
        {
            Title? entity = await _repository.GetAsync(predicate: item => item.Id.Equals(request.Id));
            await _rules.TitleShouldExistWhenSelected(entity);

            Title deletedEntity = await _repository.DeleteAsync(entity!);
            await NormalizeVisibleOrdersAsync(request.Id, cancellationToken);

            return _mapper.Map<DeletedTitleResponse>(deletedEntity);
        }

        private async Task NormalizeVisibleOrdersAsync(Guid deletedEntityId, CancellationToken cancellationToken)
        {
            List<Title> entities = _repository.Query()
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
