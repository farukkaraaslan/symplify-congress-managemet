using Core.Application.Pipelines.Authorization;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.Congresses.Constants;
using Symplify.BackOffice.Application.Features.Congresses.Constants;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.Congresses.Queries.GetForUpdate;
public class GetCongressForUpdateQuery : IRequest<GetCongressForUpdateResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { CongressesOperationClaims.Admin, CongressesOperationClaims.Read };
    public class GetCongressForUpdateQueryHandler : IRequestHandler<GetCongressForUpdateQuery, GetCongressForUpdateResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Title", "Subtitle", "Description", "LogoPath" };
        private readonly ICongressRepository _repository; private readonly ICongressTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public GetCongressForUpdateQueryHandler(ICongressRepository repository, ICongressTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<GetCongressForUpdateResponse> Handle(GetCongressForUpdateQuery request, CancellationToken cancellationToken)
        {
            Congress? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            if (entity is null) throw new BusinessException(CongressesMessages.EntityNotFound);
            IReadOnlyList<ApplicationLanguageDto> languages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            List<CongressTranslation> translations = _translationRepository.Query().ToList().Where(x => EqualityComparer<Guid>.Default.Equals(x.CongressId, request.Id)).ToList();
            return new GetCongressForUpdateResponse { Id = entity.Id,
                TenantId = entity.TenantId,
                Code = entity.Code,
                Name = entity.Name,
                Slug = entity.Slug,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Status = entity.Status,
                IsActive = entity.IsActive,
                Translations = languages.Select(language => { CongressTranslation? translation = translations.FirstOrDefault(x => x.LanguageId == language.Id); return new LocalizedTranslationDto { LanguageId = language.Id, Culture = language.Culture, LanguageName = language.Name, IsDefault = language.IsDefault, Exists = translation is not null, Fields = LocalizedEntityRuntimeHelper.ExtractFields(translation, TranslationFieldNames) }; }).ToList() };
        }
    }
}
