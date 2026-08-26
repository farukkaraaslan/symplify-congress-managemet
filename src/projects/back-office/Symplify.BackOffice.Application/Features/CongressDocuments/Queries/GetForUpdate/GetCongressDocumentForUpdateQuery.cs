using AutoMapper;
using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressDocuments.Constants;
using Symplify.BackOffice.Application.Features.CongressDocuments.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressDocuments.Queries.GetForUpdate;

public class GetCongressDocumentForUpdateQuery : IRequest<GetCongressDocumentForUpdateResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public string[] Roles => new[] { CongressDocumentsOperationClaims.Admin, CongressDocumentsOperationClaims.Read };

    public class GetCongressDocumentForUpdateQueryHandler : IRequestHandler<GetCongressDocumentForUpdateQuery, GetCongressDocumentForUpdateResponse>
    {
        private readonly ICongressDocumentRepository _repository;
        private readonly ICongressDocumentTranslationRepository _translationRepository;
        private readonly IMapper _mapper;
        private readonly CongressDocumentBusinessRules _rules;

        public GetCongressDocumentForUpdateQueryHandler(
            ICongressDocumentRepository repository,
            ICongressDocumentTranslationRepository translationRepository,
            IMapper mapper,
            CongressDocumentBusinessRules rules)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<GetCongressDocumentForUpdateResponse> Handle(GetCongressDocumentForUpdateQuery request, CancellationToken cancellationToken)
        {
            CongressDocument? entity = await _repository.GetAsync(
                predicate: item => item.Id.Equals(request.Id),
                cancellationToken: cancellationToken);

            await _rules.CongressDocumentShouldExistWhenSelected(entity);
            await _rules.DocumentShouldBelongToCongress(entity!, request.CongressId);

            GetCongressDocumentForUpdateResponse response = _mapper.Map<GetCongressDocumentForUpdateResponse>(entity);

            response.Translations = _translationRepository
                .Query()
                .ToList()
                .Where(translation =>
                    translation.CongressDocumentId == request.Id &&
                    !IsDeleted(translation))
                .Select(translation => new CongressDocumentTranslationForUpdateDto
                {
                    Id = translation.Id,
                    LanguageId = translation.LanguageId,
                    Description = translation.Description
                })
                .ToList();

            return response;
        }

        private static bool IsDeleted(object entity)
        {
            return Symplify.BackOffice.Application.Common.Localization.LocalizedEntityRuntimeHelper
                .GetPropertyValue(entity, "DeletedDate") is not null;
        }
    }
}
