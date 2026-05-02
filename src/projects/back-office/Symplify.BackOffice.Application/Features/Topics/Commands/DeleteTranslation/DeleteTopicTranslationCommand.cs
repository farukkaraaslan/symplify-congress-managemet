using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.Topics.Constants;
using Symplify.BackOffice.Application.Features.Topics.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;
namespace Symplify.BackOffice.Application.Features.Topics.Commands.DeleteTranslation;
public class DeleteTopicTranslationCommand : IRequest<DeletedTopicTranslationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid TopicId { get; set; }
    public Guid LanguageId { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetTopics";
    public string[] Roles => new[] { TopicsOperationClaims.Admin, TopicsOperationClaims.Write, TopicsOperationClaims.Delete };
    public class DeleteTopicTranslationCommandHandler : IRequestHandler<DeleteTopicTranslationCommand, DeletedTopicTranslationResponse>
    {
        private readonly ITopicTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public DeleteTopicTranslationCommandHandler(ITopicTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<DeletedTopicTranslationResponse> Handle(DeleteTopicTranslationCommand request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            if (request.LanguageId == defaultLanguage.Id) throw new BusinessException(TopicsMessages.DefaultTranslationCannotBeDeleted);
            TopicTranslation? translation = _translationRepository.Query().FirstOrDefault(x => x.TopicId.Equals(request.TopicId) && x.LanguageId == request.LanguageId);
            if (translation is null) throw new BusinessException(TopicsMessages.TranslationNotFound);
            TopicTranslation deletedTranslation = await _translationRepository.DeleteAsync(translation);
            return new DeletedTopicTranslationResponse { Id = deletedTranslation.Id, TopicId = deletedTranslation.TopicId, LanguageId = deletedTranslation.LanguageId };
        }
    }
}
