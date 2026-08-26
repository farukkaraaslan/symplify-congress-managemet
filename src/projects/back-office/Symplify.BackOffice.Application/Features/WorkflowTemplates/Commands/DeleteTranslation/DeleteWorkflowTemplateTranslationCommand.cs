using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.WorkflowTemplates.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.WorkflowTemplates.Commands.DeleteTranslation;

public class DeleteWorkflowTemplateTranslationCommand : IRequest<DeletedWorkflowTemplateTranslationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid WorkflowTemplateId { get; set; }
    public Guid LanguageId { get; set; }

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetWorkflowTemplates";
    public string[] Roles => new[] { WorkflowTemplatesOperationClaims.Admin, WorkflowTemplatesOperationClaims.Write, WorkflowTemplatesOperationClaims.Update };

    public class DeleteWorkflowTemplateTranslationCommandHandler : IRequestHandler<DeleteWorkflowTemplateTranslationCommand, DeletedWorkflowTemplateTranslationResponse>
    {
        private readonly IWorkflowTemplateTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;

        public DeleteWorkflowTemplateTranslationCommandHandler(IWorkflowTemplateTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider)
        {
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
        }

        public async Task<DeletedWorkflowTemplateTranslationResponse> Handle(DeleteWorkflowTemplateTranslationCommand request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            if (request.LanguageId == defaultLanguage.Id)
                throw new BusinessException(WorkflowTemplatesMessages.DefaultTranslationRequired);

            WorkflowTemplateTranslation? translation = await _translationRepository.GetAsync(predicate: x => x.WorkflowTemplateId == request.WorkflowTemplateId && x.LanguageId == request.LanguageId, cancellationToken: cancellationToken);
            if (translation is null)
                throw new BusinessException(WorkflowTemplatesMessages.TranslationNotFound);

            await _translationRepository.DeleteAsync(translation);
            return new DeletedWorkflowTemplateTranslationResponse { Id = translation.Id };
        }
    }
}
