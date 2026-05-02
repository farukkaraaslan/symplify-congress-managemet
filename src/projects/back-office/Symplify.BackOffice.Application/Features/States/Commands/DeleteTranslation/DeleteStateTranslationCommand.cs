using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.States.Constants;
using Symplify.BackOffice.Application.Features.States.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Reference.Translations;
namespace Symplify.BackOffice.Application.Features.States.Commands.DeleteTranslation;
public class DeleteStateTranslationCommand : IRequest<DeletedStateTranslationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid StateId { get; set; }
    public Guid LanguageId { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetStates";
    public string[] Roles => new[] { StatesOperationClaims.Admin, StatesOperationClaims.Write, StatesOperationClaims.Delete };
    public class DeleteStateTranslationCommandHandler : IRequestHandler<DeleteStateTranslationCommand, DeletedStateTranslationResponse>
    {
        private readonly IStateTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public DeleteStateTranslationCommandHandler(IStateTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<DeletedStateTranslationResponse> Handle(DeleteStateTranslationCommand request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            if (request.LanguageId == defaultLanguage.Id) throw new BusinessException(StatesMessages.DefaultTranslationCannotBeDeleted);
            StateTranslation? translation = _translationRepository.Query().FirstOrDefault(x => x.StateId.Equals(request.StateId) && x.LanguageId == request.LanguageId);
            if (translation is null) throw new BusinessException(StatesMessages.TranslationNotFound);
            StateTranslation deletedTranslation = await _translationRepository.DeleteAsync(translation);
            return new DeletedStateTranslationResponse { Id = deletedTranslation.Id, StateId = deletedTranslation.StateId, LanguageId = deletedTranslation.LanguageId };
        }
    }
}
