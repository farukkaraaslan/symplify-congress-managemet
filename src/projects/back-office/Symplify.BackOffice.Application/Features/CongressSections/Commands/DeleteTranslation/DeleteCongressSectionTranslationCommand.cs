using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressSections.Constants;
using Symplify.BackOffice.Application.Features.CongressSections.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressSections.Commands.DeleteTranslation;

public class DeleteCongressSectionTranslationCommand
    : IRequest<DeletedCongressSectionTranslationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressSectionId { get; set; }
    public Guid LanguageId { get; set; }

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressSections";

    public string[] Roles => new[]
    {
        CongressSectionsOperationClaims.Admin,
        CongressSectionsOperationClaims.Write,
        CongressSectionsOperationClaims.Delete
    };

    public class DeleteCongressSectionTranslationCommandHandler
        : IRequestHandler<DeleteCongressSectionTranslationCommand, DeletedCongressSectionTranslationResponse>
    {
        private readonly ICongressSectionTranslationRepository _translationRepository;
        private readonly CongressSectionBusinessRules _rules;

        public DeleteCongressSectionTranslationCommandHandler(
            ICongressSectionTranslationRepository translationRepository,
            CongressSectionBusinessRules rules)
        {
            _translationRepository = translationRepository;
            _rules = rules;
        }

        public async Task<DeletedCongressSectionTranslationResponse> Handle(
            DeleteCongressSectionTranslationCommand request,
            CancellationToken cancellationToken)
        {
            await _rules.DefaultTranslationCannotBeDeleted(request.LanguageId, cancellationToken);

            CongressSectionTranslation? translation = _translationRepository
                .Query()
                .FirstOrDefault(entity =>
                    entity.CongressSectionId.Equals(request.CongressSectionId) &&
                    entity.LanguageId == request.LanguageId);

            await _rules.TranslationShouldExistWhenSelected(translation);

            CongressSectionTranslation deletedTranslation =
                await _translationRepository.DeleteAsync(translation!);

            return new DeletedCongressSectionTranslationResponse
            {
                Id = deletedTranslation.Id,
                CongressSectionId = deletedTranslation.CongressSectionId,
                LanguageId = deletedTranslation.LanguageId
            };
        }
    }
}
