using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.DocumentTypes.Constants;
using Symplify.BackOffice.Application.Features.DocumentTypes.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;
namespace Symplify.BackOffice.Application.Features.DocumentTypes.Commands.DeleteTranslation;
public class DeleteDocumentTypeTranslationCommand : IRequest<DeletedDocumentTypeTranslationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid DocumentTypeId { get; set; }
    public Guid LanguageId { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetDocumentTypes";
    public string[] Roles => new[] { DocumentTypesOperationClaims.Admin, DocumentTypesOperationClaims.Write, DocumentTypesOperationClaims.Delete };
    public class DeleteDocumentTypeTranslationCommandHandler : IRequestHandler<DeleteDocumentTypeTranslationCommand, DeletedDocumentTypeTranslationResponse>
    {
        private readonly IDocumentTypeTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public DeleteDocumentTypeTranslationCommandHandler(IDocumentTypeTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<DeletedDocumentTypeTranslationResponse> Handle(DeleteDocumentTypeTranslationCommand request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            if (request.LanguageId == defaultLanguage.Id) throw new BusinessException(DocumentTypesMessages.DefaultTranslationCannotBeDeleted);
            DocumentTypeTranslation? translation = _translationRepository.Query().FirstOrDefault(x => x.DocumentTypeId.Equals(request.DocumentTypeId) && x.LanguageId == request.LanguageId);
            if (translation is null) throw new BusinessException(DocumentTypesMessages.TranslationNotFound);
            DocumentTypeTranslation deletedTranslation = await _translationRepository.DeleteAsync(translation);
            return new DeletedDocumentTypeTranslationResponse { Id = deletedTranslation.Id, DocumentTypeId = deletedTranslation.DocumentTypeId, LanguageId = deletedTranslation.LanguageId };
        }
    }
}
