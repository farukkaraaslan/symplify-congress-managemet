using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.Cities.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Reference.Translations;
using Symplify.BackOffice.Application.Features.Cities.Commands.DeleteTranslation;
namespace Symplify.BackOffice.Application.Features.Cities.Commands.DeleteTranslation;
public class DeleteCityTranslationCommand : IRequest<DeletedCityTranslationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CityId { get; set; }
    public Guid LanguageId { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCities";
    public string[] Roles => new[] { CitiesOperationClaims.Admin, CitiesOperationClaims.Write, CitiesOperationClaims.Delete };
    public class DeleteCityTranslationCommandHandler : IRequestHandler<DeleteCityTranslationCommand, DeletedCityTranslationResponse>
    {
        private readonly ICityTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public DeleteCityTranslationCommandHandler(ICityTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<DeletedCityTranslationResponse> Handle(DeleteCityTranslationCommand request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            if (request.LanguageId == defaultLanguage.Id) throw new BusinessException(CitiesMessages.DefaultTranslationCannotBeDeleted);
            CityTranslation? translation = _translationRepository.Query().FirstOrDefault(x => x.CityId.Equals(request.CityId) && x.LanguageId == request.LanguageId);
            if (translation is null) throw new BusinessException(CitiesMessages.TranslationNotFound);
            CityTranslation deletedTranslation = await _translationRepository.DeleteAsync(translation);
            return new DeletedCityTranslationResponse { Id = deletedTranslation.Id, CityId = deletedTranslation.CityId, LanguageId = deletedTranslation.LanguageId };
        }
    }
}
