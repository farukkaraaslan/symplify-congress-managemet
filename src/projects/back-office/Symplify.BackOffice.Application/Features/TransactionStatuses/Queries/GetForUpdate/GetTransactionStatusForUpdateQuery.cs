using Core.Application.Pipelines.Authorization;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.TransactionStatuses.Constants;
using Symplify.BackOffice.Application.Features.TransactionStatuses.Constants;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
using Symplify.BackOffice.Domain.Workflow;
namespace Symplify.BackOffice.Application.Features.TransactionStatuses.Queries.GetForUpdate;
public class GetTransactionStatusForUpdateQuery : IRequest<GetTransactionStatusForUpdateResponse>, ISecuredRequest
{
    public int Id { get; set; }
    public string[] Roles => new[] { TransactionStatusesOperationClaims.Admin, TransactionStatusesOperationClaims.Read };
    public class GetTransactionStatusForUpdateQueryHandler : IRequestHandler<GetTransactionStatusForUpdateQuery, GetTransactionStatusForUpdateResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name", "Description" };
        private readonly ITransactionStatusRepository _repository; private readonly ITransactionStatusTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public GetTransactionStatusForUpdateQueryHandler(ITransactionStatusRepository repository, ITransactionStatusTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<GetTransactionStatusForUpdateResponse> Handle(GetTransactionStatusForUpdateQuery request, CancellationToken cancellationToken)
        {
            TransactionStatus? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            if (entity is null) throw new BusinessException(TransactionStatusesMessages.EntityNotFound);
            IReadOnlyList<ApplicationLanguageDto> languages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            List<TransactionStatusTranslation> translations = _translationRepository.Query().ToList().Where(x => EqualityComparer<int>.Default.Equals(x.TransactionStatusId, request.Id)).ToList();
            return new GetTransactionStatusForUpdateResponse { Id = entity.Id,
                Code = entity.Code,
                Order = entity.Order,
                IsActive = entity.IsActive,
                Translations = languages.Select(language => { TransactionStatusTranslation? translation = translations.FirstOrDefault(x => x.LanguageId == language.Id); return new LocalizedTranslationDto { LanguageId = language.Id, Culture = language.Culture, LanguageName = language.Name, IsDefault = language.IsDefault, Exists = translation is not null, Fields = LocalizedEntityRuntimeHelper.ExtractFields(translation, TranslationFieldNames) }; }).ToList() };
        }
    }
}
