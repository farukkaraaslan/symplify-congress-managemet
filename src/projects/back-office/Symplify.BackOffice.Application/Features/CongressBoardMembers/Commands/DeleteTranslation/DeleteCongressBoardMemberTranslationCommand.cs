using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands.DeleteTranslation;
public class DeleteCongressBoardMemberTranslationCommand : IRequest<DeletedCongressBoardMemberTranslationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressBoardMemberId { get; set; }
    public Guid LanguageId { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressBoardMembers";
    public string[] Roles => new[] { CongressBoardMembersOperationClaims.Admin, CongressBoardMembersOperationClaims.Write, CongressBoardMembersOperationClaims.Delete };
    public class DeleteCongressBoardMemberTranslationCommandHandler : IRequestHandler<DeleteCongressBoardMemberTranslationCommand, DeletedCongressBoardMemberTranslationResponse>
    {
        private readonly ICongressBoardMemberTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public DeleteCongressBoardMemberTranslationCommandHandler(ICongressBoardMemberTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<DeletedCongressBoardMemberTranslationResponse> Handle(DeleteCongressBoardMemberTranslationCommand request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            if (request.LanguageId == defaultLanguage.Id) throw new BusinessException(CongressBoardMembersMessages.DefaultTranslationCannotBeDeleted);
            CongressBoardMemberTranslation? translation = _translationRepository.Query().FirstOrDefault(x => x.CongressBoardMemberId.Equals(request.CongressBoardMemberId) && x.LanguageId == request.LanguageId);
            if (translation is null) throw new BusinessException(CongressBoardMembersMessages.TranslationNotFound);
            CongressBoardMemberTranslation deletedTranslation = await _translationRepository.DeleteAsync(translation);
            return new DeletedCongressBoardMemberTranslationResponse { Id = deletedTranslation.Id, CongressBoardMemberId = deletedTranslation.CongressBoardMemberId, LanguageId = deletedTranslation.LanguageId };
        }
    }
}
