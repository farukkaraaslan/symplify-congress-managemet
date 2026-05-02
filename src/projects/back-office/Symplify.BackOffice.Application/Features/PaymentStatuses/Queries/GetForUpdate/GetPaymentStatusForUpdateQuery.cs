using Core.Application.Pipelines.Authorization;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.PaymentStatuses.Constants;
using Symplify.BackOffice.Application.Features.PaymentStatuses.Constants;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
using Symplify.BackOffice.Domain.Workflow;
namespace Symplify.BackOffice.Application.Features.PaymentStatuses.Queries.GetForUpdate;
public class GetPaymentStatusForUpdateQuery : IRequest<GetPaymentStatusForUpdateResponse>, ISecuredRequest
{
    public int Id { get; set; }
    public string[] Roles => new[] { PaymentStatusesOperationClaims.Admin, PaymentStatusesOperationClaims.Read };
    public class GetPaymentStatusForUpdateQueryHandler : IRequestHandler<GetPaymentStatusForUpdateQuery, GetPaymentStatusForUpdateResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name", "Description" };
        private readonly IPaymentStatusRepository _repository; private readonly IPaymentStatusTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public GetPaymentStatusForUpdateQueryHandler(IPaymentStatusRepository repository, IPaymentStatusTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<GetPaymentStatusForUpdateResponse> Handle(GetPaymentStatusForUpdateQuery request, CancellationToken cancellationToken)
        {
            PaymentStatus? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            if (entity is null) throw new BusinessException(PaymentStatusesMessages.EntityNotFound);
            IReadOnlyList<ApplicationLanguageDto> languages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            List<PaymentStatusTranslation> translations = _translationRepository.Query().ToList().Where(x => EqualityComparer<int>.Default.Equals(x.PaymentStatusId, request.Id)).ToList();
            return new GetPaymentStatusForUpdateResponse { Id = entity.Id,
                Code = entity.Code,
                Order = entity.Order,
                IsActive = entity.IsActive,
                Translations = languages.Select(language => { PaymentStatusTranslation? translation = translations.FirstOrDefault(x => x.LanguageId == language.Id); return new LocalizedTranslationDto { LanguageId = language.Id, Culture = language.Culture, LanguageName = language.Name, IsDefault = language.IsDefault, Exists = translation is not null, Fields = LocalizedEntityRuntimeHelper.ExtractFields(translation, TranslationFieldNames) }; }).ToList() };
        }
    }
}
