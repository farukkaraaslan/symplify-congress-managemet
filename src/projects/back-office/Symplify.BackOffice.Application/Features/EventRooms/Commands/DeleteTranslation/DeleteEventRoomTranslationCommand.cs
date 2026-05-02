using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.EventRooms.Constants;
using Symplify.BackOffice.Application.Features.EventRooms.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;
namespace Symplify.BackOffice.Application.Features.EventRooms.Commands.DeleteTranslation;
public class DeleteEventRoomTranslationCommand : IRequest<DeletedEventRoomTranslationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid EventRoomId { get; set; }
    public Guid LanguageId { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetEventRooms";
    public string[] Roles => new[] { EventRoomsOperationClaims.Admin, EventRoomsOperationClaims.Write, EventRoomsOperationClaims.Delete };
    public class DeleteEventRoomTranslationCommandHandler : IRequestHandler<DeleteEventRoomTranslationCommand, DeletedEventRoomTranslationResponse>
    {
        private readonly IEventRoomTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public DeleteEventRoomTranslationCommandHandler(IEventRoomTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<DeletedEventRoomTranslationResponse> Handle(DeleteEventRoomTranslationCommand request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            if (request.LanguageId == defaultLanguage.Id) throw new BusinessException(EventRoomsMessages.DefaultTranslationCannotBeDeleted);
            EventRoomTranslation? translation = _translationRepository.Query().FirstOrDefault(x => x.EventRoomId.Equals(request.EventRoomId) && x.LanguageId == request.LanguageId);
            if (translation is null) throw new BusinessException(EventRoomsMessages.TranslationNotFound);
            EventRoomTranslation deletedTranslation = await _translationRepository.DeleteAsync(translation);
            return new DeletedEventRoomTranslationResponse { Id = deletedTranslation.Id, EventRoomId = deletedTranslation.EventRoomId, LanguageId = deletedTranslation.LanguageId };
        }
    }
}
