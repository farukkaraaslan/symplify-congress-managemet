using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressSliders.Constants;
using Symplify.BackOffice.Application.Features.CongressSliders.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressSliders.Commands.DeleteTranslation;
public class DeleteCongressSliderTranslationCommand : IRequest<DeletedCongressSliderTranslationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressSliderId { get; set; }
    public Guid LanguageId { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressSliders";
    public string[] Roles => new[] { CongressSlidersOperationClaims.Admin, CongressSlidersOperationClaims.Write, CongressSlidersOperationClaims.Delete };
    public class DeleteCongressSliderTranslationCommandHandler : IRequestHandler<DeleteCongressSliderTranslationCommand, DeletedCongressSliderTranslationResponse>
    {
        private readonly ICongressSliderTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public DeleteCongressSliderTranslationCommandHandler(ICongressSliderTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<DeletedCongressSliderTranslationResponse> Handle(DeleteCongressSliderTranslationCommand request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            if (request.LanguageId == defaultLanguage.Id) throw new BusinessException(CongressSlidersMessages.DefaultTranslationCannotBeDeleted);
            CongressSliderTranslation? translation = _translationRepository.Query().FirstOrDefault(x => x.CongressSliderId.Equals(request.CongressSliderId) && x.LanguageId == request.LanguageId);
            if (translation is null) throw new BusinessException(CongressSlidersMessages.TranslationNotFound);
            CongressSliderTranslation deletedTranslation = await _translationRepository.DeleteAsync(translation);
            return new DeletedCongressSliderTranslationResponse { Id = deletedTranslation.Id, CongressSliderId = deletedTranslation.CongressSliderId, LanguageId = deletedTranslation.LanguageId };
        }
    }
}
