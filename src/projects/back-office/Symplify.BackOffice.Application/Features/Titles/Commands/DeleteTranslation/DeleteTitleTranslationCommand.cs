using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.Titles.Constants;
using Symplify.BackOffice.Application.Features.Titles.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;
namespace Symplify.BackOffice.Application.Features.Titles.Commands.DeleteTranslation;
public class DeleteTitleTranslationCommand : IRequest<DeletedTitleTranslationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid TitleId { get; set; }
    public Guid LanguageId { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetTitles";
    public string[] Roles => new[] { TitlesOperationClaims.Admin, TitlesOperationClaims.Write, TitlesOperationClaims.Delete };
    public class DeleteTitleTranslationCommandHandler : IRequestHandler<DeleteTitleTranslationCommand, DeletedTitleTranslationResponse>
    {
        private readonly ITitleTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public DeleteTitleTranslationCommandHandler(ITitleTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<DeletedTitleTranslationResponse> Handle(DeleteTitleTranslationCommand request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            if (request.LanguageId == defaultLanguage.Id) throw new BusinessException(TitlesMessages.DefaultTranslationCannotBeDeleted);
            TitleTranslation? translation = _translationRepository.Query().FirstOrDefault(x => x.TitleId.Equals(request.TitleId) && x.LanguageId == request.LanguageId);
            if (translation is null) throw new BusinessException(TitlesMessages.TranslationNotFound);
            TitleTranslation deletedTranslation = await _translationRepository.DeleteAsync(translation);
            return new DeletedTitleTranslationResponse { Id = deletedTranslation.Id, TitleId = deletedTranslation.TitleId, LanguageId = deletedTranslation.LanguageId };
        }
    }
}
