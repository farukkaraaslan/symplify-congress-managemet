using Core.Application.Pipelines.Authorization;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Queries.GetForUpdate;
public class GetCongressBoardMemberForUpdateQuery : IRequest<GetCongressBoardMemberForUpdateResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { CongressBoardMembersOperationClaims.Admin, CongressBoardMembersOperationClaims.Read };
    public class GetCongressBoardMemberForUpdateQueryHandler : IRequestHandler<GetCongressBoardMemberForUpdateQuery, GetCongressBoardMemberForUpdateResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "FullName", "Title", "Institution", "Biography" };
        private readonly ICongressBoardMemberRepository _repository; private readonly ICongressBoardMemberTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public GetCongressBoardMemberForUpdateQueryHandler(ICongressBoardMemberRepository repository, ICongressBoardMemberTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<GetCongressBoardMemberForUpdateResponse> Handle(GetCongressBoardMemberForUpdateQuery request, CancellationToken cancellationToken)
        {
            CongressBoardMember? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            if (entity is null) throw new BusinessException(CongressBoardMembersMessages.EntityNotFound);
            IReadOnlyList<ApplicationLanguageDto> languages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            List<CongressBoardMemberTranslation> translations = _translationRepository.Query().ToList().Where(x => EqualityComparer<Guid>.Default.Equals(x.CongressBoardMemberId, request.Id)).ToList();
            return new GetCongressBoardMemberForUpdateResponse { Id = entity.Id,
                CongressBoardId = entity.CongressBoardId,
                ImagePath = entity.ImagePath,
                Order = entity.Order,
                IsActive = entity.IsActive,
                Translations = languages.Select(language => { CongressBoardMemberTranslation? translation = translations.FirstOrDefault(x => x.LanguageId == language.Id); return new LocalizedTranslationDto { LanguageId = language.Id, Culture = language.Culture, LanguageName = language.Name, IsDefault = language.IsDefault, Exists = translation is not null, Fields = LocalizedEntityRuntimeHelper.ExtractFields(translation, TranslationFieldNames) }; }).ToList() };
        }
    }
}
