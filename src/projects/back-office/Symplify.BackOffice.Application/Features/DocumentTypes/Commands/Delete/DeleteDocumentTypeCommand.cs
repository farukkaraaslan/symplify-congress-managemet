using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.DocumentTypes.Constants;
using Symplify.BackOffice.Application.Features.DocumentTypes.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Application.Features.DocumentTypes.Commands.Delete;

public class DeleteDocumentTypeCommand : IRequest<DeletedDocumentTypeResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetDocumentTypes";

    public string[] Roles => new[] { DocumentTypesOperationClaims.Admin, DocumentTypesOperationClaims.Write, DocumentTypesOperationClaims.Delete };

    public class DeleteDocumentTypeCommandHandler : IRequestHandler<DeleteDocumentTypeCommand, DeletedDocumentTypeResponse>
    {
        private readonly IDocumentTypeRepository _repository;
        private readonly IMapper _mapper;
        private readonly DocumentTypeBusinessRules _rules;

        public DeleteDocumentTypeCommandHandler(
            IDocumentTypeRepository repository,
            IMapper mapper,
            DocumentTypeBusinessRules rules)
        {
            _repository = repository;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<DeletedDocumentTypeResponse> Handle(DeleteDocumentTypeCommand request, CancellationToken cancellationToken)
        {
            DocumentType? entity = await _repository.GetAsync(predicate: item => item.Id.Equals(request.Id));
            await _rules.DocumentTypeShouldExistWhenSelected(entity);

            DocumentType deletedEntity = await _repository.DeleteAsync(entity!);
            await NormalizeVisibleOrdersAsync(request.Id, cancellationToken);

            return _mapper.Map<DeletedDocumentTypeResponse>(deletedEntity);
        }

        private async Task NormalizeVisibleOrdersAsync(Guid deletedEntityId, CancellationToken cancellationToken)
        {
            List<DocumentType> entities = _repository.Query()
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
