using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressBoards.Constants;
using Symplify.BackOffice.Application.Features.CongressBoards.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressBoards.Commands.DeleteTranslation;
public class DeleteCongressBoardTranslationCommand : IRequest<DeletedCongressBoardTranslationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressBoardId { get; set; }
    public Guid LanguageId { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressBoards";
    public string[] Roles => new[] { CongressBoardsOperationClaims.Admin, CongressBoardsOperationClaims.Write, CongressBoardsOperationClaims.Delete };
    public class DeleteCongressBoardTranslationCommandHandler : IRequestHandler<DeleteCongressBoardTranslationCommand, DeletedCongressBoardTranslationResponse>
    {
        private readonly ICongressBoardTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public DeleteCongressBoardTranslationCommandHandler(ICongressBoardTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<DeletedCongressBoardTranslationResponse> Handle(DeleteCongressBoardTranslationCommand request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            if (request.LanguageId == defaultLanguage.Id) throw new BusinessException(CongressBoardsMessages.DefaultTranslationCannotBeDeleted);
            CongressBoardTranslation? translation = _translationRepository.Query().FirstOrDefault(x => x.CongressBoardId.Equals(request.CongressBoardId) && x.LanguageId == request.LanguageId);
            if (translation is null) throw new BusinessException(CongressBoardsMessages.TranslationNotFound);
            CongressBoardTranslation deletedTranslation = await _translationRepository.DeleteAsync(translation);
            return new DeletedCongressBoardTranslationResponse { Id = deletedTranslation.Id, CongressBoardId = deletedTranslation.CongressBoardId, LanguageId = deletedTranslation.LanguageId };
        }
    }
}
