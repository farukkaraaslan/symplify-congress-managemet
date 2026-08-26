using Core.Application.Pipelines.Authorization;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Queries.GetById;
public class GetByIdCongressBoardMemberQuery : IRequest<GetByIdCongressBoardMemberResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public Guid? LanguageId { get; set; }
    public string? Culture { get; set; }
    public string[] Roles => new[] { CongressBoardMembersOperationClaims.Admin, CongressBoardMembersOperationClaims.Read };
    public class GetByIdCongressBoardMemberQueryHandler : IRequestHandler<GetByIdCongressBoardMemberQuery, GetByIdCongressBoardMemberResponse>
    {
        private readonly ICongressBoardMemberRepository _repository; private readonly ICongressBoardMemberTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider; private readonly ICurrentLanguageProvider _currentLanguageProvider; private readonly ITranslationFallbackResolver _fallbackResolver;
        public GetByIdCongressBoardMemberQueryHandler(ICongressBoardMemberRepository repository, ICongressBoardMemberTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider, ICurrentLanguageProvider currentLanguageProvider, ITranslationFallbackResolver fallbackResolver) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; _currentLanguageProvider = currentLanguageProvider; _fallbackResolver = fallbackResolver; }
        public async Task<GetByIdCongressBoardMemberResponse> Handle(GetByIdCongressBoardMemberQuery request, CancellationToken cancellationToken)
        {
            CongressBoardMember? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            if (entity is null) throw new BusinessException(CongressBoardMembersMessages.EntityNotFound);
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = request.LanguageId.HasValue ? await _languageProvider.GetByIdAsync(request.LanguageId.Value, cancellationToken) ?? defaultLanguage : !string.IsNullOrWhiteSpace(request.Culture) ? await _languageProvider.GetByCultureAsync(request.Culture, cancellationToken) ?? defaultLanguage : await _currentLanguageProvider.GetCurrentLanguageAsync(cancellationToken);
            List<CongressBoardMemberTranslation> translations = _translationRepository.Query().ToList().Where(x => EqualityComparer<Guid>.Default.Equals(x.CongressBoardMemberId, request.Id)).ToList();
            CongressBoardMemberTranslation? requestedTranslation = translations.FirstOrDefault(x => x.LanguageId == requestedLanguage.Id);
            CongressBoardMemberTranslation? displayTranslation = _fallbackResolver.Resolve(translations, requestedLanguage.Id, defaultLanguage.Id);
            return new GetByIdCongressBoardMemberResponse { Id = entity.Id,
                CongressBoardId = entity.CongressBoardId,
                ImagePath = entity.ImagePath,
                Order = entity.Order,
                IsActive = entity.IsActive,
                FullName = displayTranslation is null ? string.Empty : (string)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "FullName")!,
                Title = displayTranslation is null ? null : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Title")!,
                Institution = displayTranslation is null ? null : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Institution")!,
                Biography = displayTranslation is null ? null : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Biography")!,
                DisplayLanguageId = displayTranslation?.LanguageId ?? default, IsFallback = requestedTranslation is null && displayTranslation is not null };
        }
    }
}
