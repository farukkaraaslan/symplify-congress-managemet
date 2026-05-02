using Core.Application.Pipelines.Authorization;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressPaymentPlans.Constants;
using Symplify.BackOffice.Application.Features.CongressPaymentPlans.Constants;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressPaymentPlans.Queries.GetForUpdate;
public class GetCongressPaymentPlanForUpdateQuery : IRequest<GetCongressPaymentPlanForUpdateResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { CongressPaymentPlansOperationClaims.Admin, CongressPaymentPlansOperationClaims.Read };
    public class GetCongressPaymentPlanForUpdateQueryHandler : IRequestHandler<GetCongressPaymentPlanForUpdateQuery, GetCongressPaymentPlanForUpdateResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name", "Description" };
        private readonly ICongressPaymentPlanRepository _repository; private readonly ICongressPaymentPlanTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public GetCongressPaymentPlanForUpdateQueryHandler(ICongressPaymentPlanRepository repository, ICongressPaymentPlanTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<GetCongressPaymentPlanForUpdateResponse> Handle(GetCongressPaymentPlanForUpdateQuery request, CancellationToken cancellationToken)
        {
            CongressPaymentPlan? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            if (entity is null) throw new BusinessException(CongressPaymentPlansMessages.EntityNotFound);
            IReadOnlyList<ApplicationLanguageDto> languages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            List<CongressPaymentPlanTranslation> translations = _translationRepository.Query().ToList().Where(x => EqualityComparer<Guid>.Default.Equals(x.CongressPaymentPlanId, request.Id)).ToList();
            return new GetCongressPaymentPlanForUpdateResponse { Id = entity.Id,
                CongressId = entity.CongressId,
                Amount = entity.Amount,
                Currency = entity.Currency,
                DueDate = entity.DueDate,
                Order = entity.Order,
                IsActive = entity.IsActive,
                Translations = languages.Select(language => { CongressPaymentPlanTranslation? translation = translations.FirstOrDefault(x => x.LanguageId == language.Id); return new LocalizedTranslationDto { LanguageId = language.Id, Culture = language.Culture, LanguageName = language.Name, IsDefault = language.IsDefault, Exists = translation is not null, Fields = LocalizedEntityRuntimeHelper.ExtractFields(translation, TranslationFieldNames) }; }).ToList() };
        }
    }
}
