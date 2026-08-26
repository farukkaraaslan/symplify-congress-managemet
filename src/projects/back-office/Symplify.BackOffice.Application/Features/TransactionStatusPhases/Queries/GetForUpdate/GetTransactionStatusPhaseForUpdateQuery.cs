using Core.Application.Pipelines.Authorization;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.TransactionStatusPhases.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.TransactionStatusPhases.Queries.GetForUpdate;

public class GetTransactionStatusPhaseForUpdateQuery
    : IRequest<GetTransactionStatusPhaseForUpdateResponse>, ISecuredRequest
{
    public int Id { get; set; }

    public string[] Roles => new[] { TransactionStatusPhasesOperationClaims.Admin, TransactionStatusPhasesOperationClaims.Read };

    public class GetTransactionStatusPhaseForUpdateQueryHandler
        : IRequestHandler<GetTransactionStatusPhaseForUpdateQuery, GetTransactionStatusPhaseForUpdateResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name", "Description" };

        private readonly ITransactionStatusPhaseRepository _repository;
        private readonly ITransactionStatusPhaseTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;

        public GetTransactionStatusPhaseForUpdateQueryHandler(
            ITransactionStatusPhaseRepository repository,
            ITransactionStatusPhaseTranslationRepository translationRepository,
            IApplicationLanguageProvider languageProvider)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
        }

        public async Task<GetTransactionStatusPhaseForUpdateResponse> Handle(
            GetTransactionStatusPhaseForUpdateQuery request,
            CancellationToken cancellationToken)
        {
            TransactionStatusPhase? entity = await _repository.GetAsync(predicate: root => root.Id.Equals(request.Id));

            if (entity is null)
                throw new BusinessException(TransactionStatusPhasesMessages.EntityNotFound);

            IReadOnlyList<ApplicationLanguageDto> languages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);

            List<TransactionStatusPhaseTranslation> translations = _translationRepository.Query()
                .ToList()
                .Where(translation => translation.TransactionStatusPhaseId == request.Id)
                .ToList();

            return new GetTransactionStatusPhaseForUpdateResponse
            {
                Id = entity.Id,
                Code = entity.Code,
                Order = entity.Order,
                IsActive = entity.IsActive,
                Translations = languages.Select(language =>
                {
                    TransactionStatusPhaseTranslation? translation = translations.FirstOrDefault(item => item.LanguageId == language.Id);

                    return new LocalizedTranslationDto
                    {
                        LanguageId = language.Id,
                        Culture = language.Culture,
                        LanguageName = language.Name,
                        IsDefault = language.IsDefault,
                        Exists = translation is not null,
                        Fields = LocalizedEntityRuntimeHelper.ExtractFields(translation, TranslationFieldNames)
                    };
                }).ToList()
            };
        }
    }
}
