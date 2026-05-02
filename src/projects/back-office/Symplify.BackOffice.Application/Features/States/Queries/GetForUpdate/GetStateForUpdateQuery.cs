using Core.Application.Pipelines.Authorization;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.States.Constants;
using Symplify.BackOffice.Application.Features.States.Constants;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Reference;
using Symplify.BackOffice.Domain.Reference.Translations;
namespace Symplify.BackOffice.Application.Features.States.Queries.GetForUpdate;
public class GetStateForUpdateQuery : IRequest<GetStateForUpdateResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { StatesOperationClaims.Admin, StatesOperationClaims.Read };
    public class GetStateForUpdateQueryHandler : IRequestHandler<GetStateForUpdateQuery, GetStateForUpdateResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name" };
        private readonly IStateRepository _repository; private readonly IStateTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public GetStateForUpdateQueryHandler(IStateRepository repository, IStateTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<GetStateForUpdateResponse> Handle(GetStateForUpdateQuery request, CancellationToken cancellationToken)
        {
            State? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            if (entity is null) throw new BusinessException(StatesMessages.EntityNotFound);
            IReadOnlyList<ApplicationLanguageDto> languages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            List<StateTranslation> translations = _translationRepository.Query().ToList().Where(x => EqualityComparer<Guid>.Default.Equals(x.StateId, request.Id)).ToList();
            return new GetStateForUpdateResponse { Id = entity.Id,
                CountryId = entity.CountryId,
                Code = entity.Code,
                IsActive = entity.IsActive,
                Order = entity.Order,
                Translations = languages.Select(language => { StateTranslation? translation = translations.FirstOrDefault(x => x.LanguageId == language.Id); return new LocalizedTranslationDto { LanguageId = language.Id, Culture = language.Culture, LanguageName = language.Name, IsDefault = language.IsDefault, Exists = translation is not null, Fields = LocalizedEntityRuntimeHelper.ExtractFields(translation, TranslationFieldNames) }; }).ToList() };
        }
    }
}
