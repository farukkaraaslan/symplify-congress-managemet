using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.Regions.Constants;
using Symplify.BackOffice.Application.Features.Regions.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Reference.Translations;
namespace Symplify.BackOffice.Application.Features.Regions.Commands.DeleteTranslation;
public class DeleteRegionTranslationCommand : IRequest<DeletedRegionTranslationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid RegionId { get; set; }
    public Guid LanguageId { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetRegions";
    public string[] Roles => new[] { RegionsOperationClaims.Admin, RegionsOperationClaims.Write, RegionsOperationClaims.Delete };
    public class DeleteRegionTranslationCommandHandler : IRequestHandler<DeleteRegionTranslationCommand, DeletedRegionTranslationResponse>
    {
        private readonly IRegionTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public DeleteRegionTranslationCommandHandler(IRegionTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<DeletedRegionTranslationResponse> Handle(DeleteRegionTranslationCommand request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            if (request.LanguageId == defaultLanguage.Id) throw new BusinessException(RegionsMessages.DefaultTranslationCannotBeDeleted);
            RegionTranslation? translation = _translationRepository.Query().FirstOrDefault(x => x.RegionId.Equals(request.RegionId) && x.LanguageId == request.LanguageId);
            if (translation is null) throw new BusinessException(RegionsMessages.TranslationNotFound);
            RegionTranslation deletedTranslation = await _translationRepository.DeleteAsync(translation);
            return new DeletedRegionTranslationResponse { Id = deletedTranslation.Id, RegionId = deletedTranslation.RegionId, LanguageId = deletedTranslation.LanguageId };
        }
    }
}
