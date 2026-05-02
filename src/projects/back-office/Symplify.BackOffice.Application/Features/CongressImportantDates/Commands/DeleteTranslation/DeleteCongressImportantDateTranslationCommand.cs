using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressImportantDates.Constants;
using Symplify.BackOffice.Application.Features.CongressImportantDates.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressImportantDates.Commands.DeleteTranslation;
public class DeleteCongressImportantDateTranslationCommand : IRequest<DeletedCongressImportantDateTranslationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressImportantDateId { get; set; }
    public Guid LanguageId { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressImportantDates";
    public string[] Roles => new[] { CongressImportantDatesOperationClaims.Admin, CongressImportantDatesOperationClaims.Write, CongressImportantDatesOperationClaims.Delete };
    public class DeleteCongressImportantDateTranslationCommandHandler : IRequestHandler<DeleteCongressImportantDateTranslationCommand, DeletedCongressImportantDateTranslationResponse>
    {
        private readonly ICongressImportantDateTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public DeleteCongressImportantDateTranslationCommandHandler(ICongressImportantDateTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<DeletedCongressImportantDateTranslationResponse> Handle(DeleteCongressImportantDateTranslationCommand request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            if (request.LanguageId == defaultLanguage.Id) throw new BusinessException(CongressImportantDatesMessages.DefaultTranslationCannotBeDeleted);
            CongressImportantDateTranslation? translation = _translationRepository.Query().FirstOrDefault(x => x.CongressImportantDateId.Equals(request.CongressImportantDateId) && x.LanguageId == request.LanguageId);
            if (translation is null) throw new BusinessException(CongressImportantDatesMessages.TranslationNotFound);
            CongressImportantDateTranslation deletedTranslation = await _translationRepository.DeleteAsync(translation);
            return new DeletedCongressImportantDateTranslationResponse { Id = deletedTranslation.Id, CongressImportantDateId = deletedTranslation.CongressImportantDateId, LanguageId = deletedTranslation.LanguageId };
        }
    }
}
