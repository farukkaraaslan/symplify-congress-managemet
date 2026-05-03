using Core.Application.Pipelines.Authorization;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Queries.GetForUpdate;

public class GetTransactionStatusTransitionForUpdateQuery
    : IRequest<GetTransactionStatusTransitionForUpdateResponse>, ISecuredRequest
{
    public int Id { get; set; }

    public string[] Roles => new[] { TransactionStatusTransitionsOperationClaims.Admin, TransactionStatusTransitionsOperationClaims.Read };

    public class GetTransactionStatusTransitionForUpdateQueryHandler
        : IRequestHandler<GetTransactionStatusTransitionForUpdateQuery, GetTransactionStatusTransitionForUpdateResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name", "Description" };

        private readonly ITransactionStatusTransitionRepository _repository;
        private readonly ITransactionStatusTransitionTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;

        public GetTransactionStatusTransitionForUpdateQueryHandler(
            ITransactionStatusTransitionRepository repository,
            ITransactionStatusTransitionTranslationRepository translationRepository,
            IApplicationLanguageProvider languageProvider)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
        }

        public async Task<GetTransactionStatusTransitionForUpdateResponse> Handle(
            GetTransactionStatusTransitionForUpdateQuery request,
            CancellationToken cancellationToken)
        {
            TransactionStatusTransition? entity = await _repository.GetAsync(predicate: root => root.Id.Equals(request.Id));

            if (entity is null)
                throw new BusinessException(TransactionStatusTransitionsMessages.EntityNotFound);

            IReadOnlyList<ApplicationLanguageDto> languages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);

            List<TransactionStatusTransitionTranslation> translations = _translationRepository.Query()
                .ToList()
                .Where(translation => translation.TransactionStatusTransitionId == request.Id)
                .ToList();

            return new GetTransactionStatusTransitionForUpdateResponse
            {
                Id = entity.Id,
                FromStatusId = entity.FromStatusId,
                ToStatusId = entity.ToStatusId,
                IsActive = entity.IsActive,
                Translations = languages.Select(language =>
                {
                    TransactionStatusTransitionTranslation? translation = translations.FirstOrDefault(item => item.LanguageId == language.Id);

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
