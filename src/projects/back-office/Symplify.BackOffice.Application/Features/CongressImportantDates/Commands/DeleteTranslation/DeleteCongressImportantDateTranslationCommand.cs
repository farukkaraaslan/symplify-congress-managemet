using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressImportantDates.Constants;
using Symplify.BackOffice.Application.Features.CongressImportantDates.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressImportantDates.Commands.DeleteTranslation;

public class DeleteCongressImportantDateTranslationCommand
    : IRequest<DeletedCongressImportantDateTranslationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressImportantDateId { get; set; }

    public Guid LanguageId { get; set; }

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetCongressImportantDates";

    public string[] Roles => new[]
    {
        CongressImportantDatesOperationClaims.Admin,
        CongressImportantDatesOperationClaims.Write,
        CongressImportantDatesOperationClaims.Delete
    };

    public class DeleteCongressImportantDateTranslationCommandHandler
        : IRequestHandler<DeleteCongressImportantDateTranslationCommand, DeletedCongressImportantDateTranslationResponse>
    {
        private readonly ICongressImportantDateTranslationRepository _translationRepository;
        private readonly CongressImportantDateBusinessRules _rules;

        public DeleteCongressImportantDateTranslationCommandHandler(
            ICongressImportantDateTranslationRepository translationRepository,
            CongressImportantDateBusinessRules rules)
        {
            _translationRepository = translationRepository;
            _rules = rules;
        }

        public async Task<DeletedCongressImportantDateTranslationResponse> Handle(
            DeleteCongressImportantDateTranslationCommand request,
            CancellationToken cancellationToken)
        {
            await _rules.DefaultTranslationCannotBeDeleted(
                request.LanguageId,
                cancellationToken);

            CongressImportantDateTranslation? translation = _translationRepository
                .Query()
                .ToList()
                .FirstOrDefault(item =>
                    item.CongressImportantDateId.Equals(request.CongressImportantDateId) &&
                    item.LanguageId == request.LanguageId &&
                    !IsDeleted(item));

            await _rules.TranslationShouldExistWhenSelected(translation);

            CongressImportantDateTranslation deletedTranslation =
                await _translationRepository.DeleteAsync(translation!);

            return new DeletedCongressImportantDateTranslationResponse
            {
                Id = deletedTranslation.Id,
                CongressImportantDateId = deletedTranslation.CongressImportantDateId,
                LanguageId = deletedTranslation.LanguageId
            };
        }

        private static bool IsDeleted(object entity)
        {
            object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(
                entity,
                "DeletedDate");

            return deletedDate is not null;
        }
    }
}