using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.SubmissionTypes.Constants;
using Symplify.BackOffice.Application.Features.SubmissionTypes.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;
namespace Symplify.BackOffice.Application.Features.SubmissionTypes.Commands.DeleteTranslation;
public class DeleteSubmissionTypeTranslationCommand : IRequest<DeletedSubmissionTypeTranslationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid SubmissionTypeId { get; set; }
    public Guid LanguageId { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetSubmissionTypes";
    public string[] Roles => new[] { SubmissionTypesOperationClaims.Admin, SubmissionTypesOperationClaims.Write, SubmissionTypesOperationClaims.Delete };
    public class DeleteSubmissionTypeTranslationCommandHandler : IRequestHandler<DeleteSubmissionTypeTranslationCommand, DeletedSubmissionTypeTranslationResponse>
    {
        private readonly ISubmissionTypeTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public DeleteSubmissionTypeTranslationCommandHandler(ISubmissionTypeTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<DeletedSubmissionTypeTranslationResponse> Handle(DeleteSubmissionTypeTranslationCommand request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            if (request.LanguageId == defaultLanguage.Id) throw new BusinessException(SubmissionTypesMessages.DefaultTranslationCannotBeDeleted);
            SubmissionTypeTranslation? translation = _translationRepository.Query().FirstOrDefault(x => x.SubmissionTypeId.Equals(request.SubmissionTypeId) && x.LanguageId == request.LanguageId);
            if (translation is null) throw new BusinessException(SubmissionTypesMessages.TranslationNotFound);
            SubmissionTypeTranslation deletedTranslation = await _translationRepository.DeleteAsync(translation);
            return new DeletedSubmissionTypeTranslationResponse { Id = deletedTranslation.Id, SubmissionTypeId = deletedTranslation.SubmissionTypeId, LanguageId = deletedTranslation.LanguageId };
        }
    }
}
