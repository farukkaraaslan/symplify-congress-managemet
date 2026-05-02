using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.Countries.Constants;
using Symplify.BackOffice.Application.Features.Countries.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Reference.Translations;
namespace Symplify.BackOffice.Application.Features.Countries.Commands.DeleteTranslation;
public class DeleteCountryTranslationCommand : IRequest<DeletedCountryTranslationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CountryId { get; set; }
    public Guid LanguageId { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCountries";
    public string[] Roles => new[] { CountriesOperationClaims.Admin, CountriesOperationClaims.Write, CountriesOperationClaims.Delete };
    public class DeleteCountryTranslationCommandHandler : IRequestHandler<DeleteCountryTranslationCommand, DeletedCountryTranslationResponse>
    {
        private readonly ICountryTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public DeleteCountryTranslationCommandHandler(ICountryTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<DeletedCountryTranslationResponse> Handle(DeleteCountryTranslationCommand request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            if (request.LanguageId == defaultLanguage.Id) throw new BusinessException(CountriesMessages.DefaultTranslationCannotBeDeleted);
            CountryTranslation? translation = _translationRepository.Query().FirstOrDefault(x => x.CountryId.Equals(request.CountryId) && x.LanguageId == request.LanguageId);
            if (translation is null) throw new BusinessException(CountriesMessages.TranslationNotFound);
            CountryTranslation deletedTranslation = await _translationRepository.DeleteAsync(translation);
            return new DeletedCountryTranslationResponse { Id = deletedTranslation.Id, CountryId = deletedTranslation.CountryId, LanguageId = deletedTranslation.LanguageId };
        }
    }
}
