using Core.Application.Pipelines.Authorization;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.SubmissionTypes.Constants;
using Symplify.BackOffice.Application.Features.SubmissionTypes.Constants;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;
using Symplify.BackOffice.Domain.Lookups;
namespace Symplify.BackOffice.Application.Features.SubmissionTypes.Queries.GetById;
public class GetByIdSubmissionTypeQuery : IRequest<GetByIdSubmissionTypeResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public Guid? LanguageId { get; set; }
    public string? Culture { get; set; }
    public string[] Roles => new[] { SubmissionTypesOperationClaims.Admin, SubmissionTypesOperationClaims.Read };
    public class GetByIdSubmissionTypeQueryHandler : IRequestHandler<GetByIdSubmissionTypeQuery, GetByIdSubmissionTypeResponse>
    {
        private readonly ISubmissionTypeRepository _repository; private readonly ISubmissionTypeTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider; private readonly ICurrentLanguageProvider _currentLanguageProvider; private readonly ITranslationFallbackResolver _fallbackResolver;
        public GetByIdSubmissionTypeQueryHandler(ISubmissionTypeRepository repository, ISubmissionTypeTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider, ICurrentLanguageProvider currentLanguageProvider, ITranslationFallbackResolver fallbackResolver) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; _currentLanguageProvider = currentLanguageProvider; _fallbackResolver = fallbackResolver; }
        public async Task<GetByIdSubmissionTypeResponse> Handle(GetByIdSubmissionTypeQuery request, CancellationToken cancellationToken)
        {
            SubmissionType? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            if (entity is null) throw new BusinessException(SubmissionTypesMessages.EntityNotFound);
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = request.LanguageId.HasValue ? await _languageProvider.GetByIdAsync(request.LanguageId.Value, cancellationToken) ?? defaultLanguage : !string.IsNullOrWhiteSpace(request.Culture) ? await _languageProvider.GetByCultureAsync(request.Culture, cancellationToken) ?? defaultLanguage : await _currentLanguageProvider.GetCurrentLanguageAsync(cancellationToken);
            List<SubmissionTypeTranslation> translations = _translationRepository.Query().ToList().Where(x => EqualityComparer<Guid>.Default.Equals(x.SubmissionTypeId, request.Id)).ToList();
            SubmissionTypeTranslation? requestedTranslation = translations.FirstOrDefault(x => x.LanguageId == requestedLanguage.Id);
            SubmissionTypeTranslation? displayTranslation = _fallbackResolver.Resolve(translations, requestedLanguage.Id, defaultLanguage.Id);
            return new GetByIdSubmissionTypeResponse { Id = entity.Id,
                Code = entity.Code,
                Order = entity.Order,
                IsActive = entity.IsActive,
                Name = displayTranslation is null ? string.Empty : (string)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Name")!,
                Description = displayTranslation is null ? null : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Description")!,
                DisplayLanguageId = displayTranslation?.LanguageId ?? default, IsFallback = requestedTranslation is null && displayTranslation is not null };
        }
    }
}
