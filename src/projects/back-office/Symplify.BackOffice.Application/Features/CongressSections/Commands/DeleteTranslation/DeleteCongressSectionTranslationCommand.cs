using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressSections.Constants;
using Symplify.BackOffice.Application.Features.CongressSections.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressSections.Commands.DeleteTranslation;
public class DeleteCongressSectionTranslationCommand : IRequest<DeletedCongressSectionTranslationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressSectionId { get; set; }
    public Guid LanguageId { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressSections";
    public string[] Roles => new[] { CongressSectionsOperationClaims.Admin, CongressSectionsOperationClaims.Write, CongressSectionsOperationClaims.Delete };
    public class DeleteCongressSectionTranslationCommandHandler : IRequestHandler<DeleteCongressSectionTranslationCommand, DeletedCongressSectionTranslationResponse>
    {
        private readonly ICongressSectionTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public DeleteCongressSectionTranslationCommandHandler(ICongressSectionTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<DeletedCongressSectionTranslationResponse> Handle(DeleteCongressSectionTranslationCommand request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            if (request.LanguageId == defaultLanguage.Id) throw new BusinessException(CongressSectionsMessages.DefaultTranslationCannotBeDeleted);
            CongressSectionTranslation? translation = _translationRepository.Query().FirstOrDefault(x => x.CongressSectionId.Equals(request.CongressSectionId) && x.LanguageId == request.LanguageId);
            if (translation is null) throw new BusinessException(CongressSectionsMessages.TranslationNotFound);
            CongressSectionTranslation deletedTranslation = await _translationRepository.DeleteAsync(translation);
            return new DeletedCongressSectionTranslationResponse { Id = deletedTranslation.Id, CongressSectionId = deletedTranslation.CongressSectionId, LanguageId = deletedTranslation.LanguageId };
        }
    }
}
