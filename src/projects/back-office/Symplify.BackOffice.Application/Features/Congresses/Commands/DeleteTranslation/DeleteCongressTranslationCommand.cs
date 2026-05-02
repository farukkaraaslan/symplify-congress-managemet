using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.Congresses.Constants;
using Symplify.BackOffice.Application.Features.Congresses.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.Congresses.Commands.DeleteTranslation;
public class DeleteCongressTranslationCommand : IRequest<DeletedCongressTranslationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressId { get; set; }
    public Guid LanguageId { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongresses";
    public string[] Roles => new[] { CongressesOperationClaims.Admin, CongressesOperationClaims.Write, CongressesOperationClaims.Delete };
    public class DeleteCongressTranslationCommandHandler : IRequestHandler<DeleteCongressTranslationCommand, DeletedCongressTranslationResponse>
    {
        private readonly ICongressTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public DeleteCongressTranslationCommandHandler(ICongressTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<DeletedCongressTranslationResponse> Handle(DeleteCongressTranslationCommand request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            if (request.LanguageId == defaultLanguage.Id) throw new BusinessException(CongressesMessages.DefaultTranslationCannotBeDeleted);
            CongressTranslation? translation = _translationRepository.Query().FirstOrDefault(x => x.CongressId.Equals(request.CongressId) && x.LanguageId == request.LanguageId);
            if (translation is null) throw new BusinessException(CongressesMessages.TranslationNotFound);
            CongressTranslation deletedTranslation = await _translationRepository.DeleteAsync(translation);
            return new DeletedCongressTranslationResponse { Id = deletedTranslation.Id, CongressId = deletedTranslation.CongressId, LanguageId = deletedTranslation.LanguageId };
        }
    }
}
