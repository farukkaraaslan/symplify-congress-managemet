using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Text;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Features.Submissions.Commands.Create;
using Symplify.BackOffice.Application.Features.Submissions.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Application.Services.Workflow;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Application.Features.ExhibitionApplications.Commands.Create;

public sealed class CreateExhibitionApplicationCommand : IRequest<CreatedExhibitionApplicationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressId { get; set; }

    public Guid? SubmissionTypeId { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public string WorkName { get; set; } = string.Empty;

    public string? Dimensions { get; set; }

    public string Technique { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Address { get; set; } = string.Empty;

    public ExhibitionApplicationFileInputDto? File { get; set; }

    public List<SubmissionAuthorInputDto> Authors { get; set; } = new();

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetSubmissions";

    public string[] Roles => new[] { SubmissionsOperationClaims.Admin, SubmissionsOperationClaims.Write, SubmissionsOperationClaims.Add };

    public sealed class CreateExhibitionApplicationCommandHandler : IRequestHandler<CreateExhibitionApplicationCommand, CreatedExhibitionApplicationResponse>
    {
        private readonly ISubmissionRepository _submissionRepository;
        private readonly ICongressRepository _congressRepository;
        private readonly ICongressSubmissionTypeRepository _congressSubmissionTypeRepository;
        private readonly ICongressWorkflowSettingRepository _congressWorkflowSettingRepository;
        private readonly ICongressTransactionStatusTransitionRepository _congressTransactionStatusTransitionRepository;
        private readonly ITransactionStatusRepository _transactionStatusRepository;

        public CreateExhibitionApplicationCommandHandler(
            ISubmissionRepository submissionRepository,
            ICongressRepository congressRepository,
            ICongressSubmissionTypeRepository congressSubmissionTypeRepository,
            ICongressWorkflowSettingRepository congressWorkflowSettingRepository,
            ICongressTransactionStatusTransitionRepository congressTransactionStatusTransitionRepository,
            ITransactionStatusRepository transactionStatusRepository)
        {
            _submissionRepository = submissionRepository;
            _congressRepository = congressRepository;
            _congressSubmissionTypeRepository = congressSubmissionTypeRepository;
            _congressWorkflowSettingRepository = congressWorkflowSettingRepository;
            _congressTransactionStatusTransitionRepository = congressTransactionStatusTransitionRepository;
            _transactionStatusRepository = transactionStatusRepository;
        }

        public async Task<CreatedExhibitionApplicationResponse> Handle(CreateExhibitionApplicationCommand request, CancellationToken cancellationToken)
        {
            Congress? congress = await _congressRepository.GetAsync(
                predicate: entity => entity.Id == request.CongressId,
                cancellationToken: cancellationToken);

            if (congress is null || IsDeleted(congress))
                throw new InvalidOperationException(SubmissionsMessages.CongressNotFound);

            await EnsureExhibitionSubmissionTypeIsBoundToCongressAsync(request, cancellationToken);

            CongressWorkflowSetting? workflowSetting = await _congressWorkflowSettingRepository.GetAsync(
                predicate: entity => entity.CongressId == request.CongressId && entity.IsActive,
                cancellationToken: cancellationToken);

            if (workflowSetting?.InitialTransactionStatusId is null)
                throw new InvalidOperationException(SubmissionsMessages.CongressWorkflowNotConfigured);

            List<SubmissionAuthorInputDto> authors = NormalizeAuthors(request.Authors);

            if (!authors.Any(author => author.IsCorrespondingAuthor))
                throw new InvalidOperationException(SubmissionsMessages.CorrespondingAuthorRequired);

            DateTime now = DateTime.UtcNow;
            string auditActor = request.CreatedByUserId?.ToString() ?? "system";
            int initialStatusId = workflowSetting.InitialTransactionStatusId.Value;
            int targetStatusId = await ResolveSubmittedStatusIdAsync(initialStatusId, cancellationToken);
            CongressTransactionStatusTransition? submitTransition = initialStatusId == targetStatusId
                ? null
                : await TryGetInitialSubmitTransitionAsync(request.CongressId, initialStatusId, targetStatusId, cancellationToken);

            Guid submissionId = Guid.NewGuid();
            string submissionNumber = GenerateSubmissionNumber(submissionId);
            string workName = BackOfficeTextNormalizer.NormalizeRequiredSubmissionTitleTr(request.WorkName);

            Submission submission = new()
            {
                Id = submissionId,
                CongressId = request.CongressId,
                SubmissionTypeId = request.SubmissionTypeId,
                TopicId = null,
                CreatedByUserId = request.CreatedByUserId,
                TransactionStatusId = targetStatusId,
                SubmissionNumber = submissionNumber,
                Title = workName,
                Abstract = NormalizeOptional(request.Description),
                Keywords = NormalizeOptional(request.Technique),
                IsSubmitted = true,
                SubmittedAt = now,
                CreatedDate = now,
                CreatedBy = auditActor,
                ExhibitionDetail = new SubmissionExhibitionDetail
                {
                    Id = Guid.NewGuid(),
                    SubmissionId = submissionId,
                    WorkName = workName,
                    Dimensions = NormalizeOptional(request.Dimensions),
                    Technique = request.Technique.Trim(),
                    Description = NormalizeOptional(request.Description),
                    Address = request.Address.Trim(),
                    CreatedDate = now,
                    CreatedBy = auditActor
                }
            };

            if (request.File is not null && !string.IsNullOrWhiteSpace(request.File.FilePath))
            {
                submission.Files.Add(new SubmissionFile
                {
                    Id = Guid.NewGuid(),
                    FileKind = SubmissionFileKind.ExhibitionImage,
                    OriginalFileName = request.File.OriginalFileName.Trim(),
                    FilePath = request.File.FilePath.Trim(),
                    ContentType = NormalizeOptional(request.File.ContentType),
                    FileSize = request.File.FileSize,
                    IsActive = true,
                    CreatedDate = now,
                    CreatedBy = auditActor
                });
            }

            foreach (SubmissionAuthorInputDto authorInput in authors)
            {
                (string firstName, string lastName) = ResolveAuthorName(authorInput);

                submission.Authors.Add(new Author
                {
                    Id = Guid.NewGuid(),
                    FirstName = firstName,
                    LastName = lastName,
                    TitleId = NormalizeOptionalGuid(authorInput.TitleId),
                    Email = NormalizeOptional(authorInput.Email),
                    Institution = BackOfficeTextNormalizer.NormalizeInstitution(authorInput.Institution),
                    Orcid = NormalizeOptional(authorInput.Orcid),
                    IsCorrespondingAuthor = authorInput.IsCorrespondingAuthor,
                    CreatedDate = now,
                    CreatedBy = auditActor
                });
            }

            submission.Histories.Add(new SubmissionHistory
            {
                Id = Guid.NewGuid(),
                SubmissionId = submission.Id,
                FromStatusId = null,
                ToStatusId = targetStatusId,
                TransactionStatusTransitionId = submitTransition?.TransactionStatusTransitionId,
                PerformedByUserId = request.CreatedByUserId,
                Note = SubmissionsMessages.SubmissionCreatedAndSubmittedHistoryNote,
                PerformedAt = now,
                IsAutomatic = false,
                CreatedDate = now,
                CreatedBy = auditActor
            });

            Submission createdEntity = await _submissionRepository.AddAsync(submission);

            return new CreatedExhibitionApplicationResponse
            {
                Id = createdEntity.Id,
                SubmissionNumber = createdEntity.SubmissionNumber,
                SubmissionTypeId = createdEntity.SubmissionTypeId,
                IsSubmitted = createdEntity.IsSubmitted,
                SubmittedAt = createdEntity.SubmittedAt
            };
        }

        private async Task EnsureExhibitionSubmissionTypeIsBoundToCongressAsync(CreateExhibitionApplicationCommand request, CancellationToken cancellationToken)
        {
            if (!request.SubmissionTypeId.HasValue || request.SubmissionTypeId.Value == Guid.Empty)
                throw new InvalidOperationException(SubmissionsMessages.SubmissionTypeRequired);

            CongressSubmissionType? relation = await _congressSubmissionTypeRepository
                .Query()
                .Include(entity => entity.SubmissionType)
                .FirstOrDefaultAsync(entity =>
                        entity.CongressId == request.CongressId &&
                        entity.SubmissionTypeId == request.SubmissionTypeId.Value &&
                        entity.IsActive &&
                        entity.DeletedDate == null,
                    cancellationToken);

            if (relation is null || relation.SubmissionType.DeletedDate != null || !relation.SubmissionType.IsActive)
                throw new InvalidOperationException(SubmissionsMessages.SubmissionTypeNotAvailableForCongress);

            if (relation.SubmissionType.FormProfile != SubmissionFormProfile.ExhibitionApplication)
                throw new InvalidOperationException(SubmissionsMessages.ExhibitionSubmissionTypeRequired);
        }

        private async Task<int> ResolveSubmittedStatusIdAsync(int fallbackStatusId, CancellationToken cancellationToken)
        {
            var submittedStatus = await _transactionStatusRepository.GetAsync(
                predicate: status => status.Code == SubmissionWorkflowStatusCodes.Submitted &&
                                     status.IsActive &&
                                     status.DeletedDate == null,
                cancellationToken: cancellationToken);

            return submittedStatus?.Id ?? fallbackStatusId;
        }

        private async Task<CongressTransactionStatusTransition?> TryGetInitialSubmitTransitionAsync(
            Guid congressId,
            int initialStatusId,
            int submittedStatusId,
            CancellationToken cancellationToken)
        {
            return await _congressTransactionStatusTransitionRepository
                .Query()
                .Include(entity => entity.TransactionStatusTransition)
                .Where(entity =>
                    entity.CongressId == congressId &&
                    entity.IsActive &&
                    entity.DeletedDate == null &&
                    entity.TransactionStatusTransition.IsActive &&
                    !entity.TransactionStatusTransition.IsAuto &&
                    entity.TransactionStatusTransition.DeletedDate == null &&
                    entity.TransactionStatusTransition.FromStatusId == initialStatusId &&
                    entity.TransactionStatusTransition.ToStatusId == submittedStatusId)
                .OrderBy(entity => entity.Order)
                .ThenBy(entity => entity.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static string GenerateSubmissionNumber(Guid submissionId)
            => submissionId.ToString("N")[..8].ToUpperInvariant();

        private static List<SubmissionAuthorInputDto> NormalizeAuthors(IEnumerable<SubmissionAuthorInputDto>? authors)
        {
            if (authors is null)
                return new List<SubmissionAuthorInputDto>();

            return authors
                .Where(author => !string.IsNullOrWhiteSpace(author.FullName))
                .Select(author => new SubmissionAuthorInputDto
                {
                    TitleId = NormalizeOptionalGuid(author.TitleId),
                    FirstName = BackOfficeTextNormalizer.NormalizePersonFirstName(author.FirstName),
                    LastName = BackOfficeTextNormalizer.NormalizePersonSurname(author.LastName),
                    FullName = NormalizeAuthorFullName(author),
                    Email = NormalizeOptional(author.Email),
                    Institution = BackOfficeTextNormalizer.NormalizeInstitution(author.Institution),
                    Orcid = NormalizeOptional(author.Orcid),
                    IsCorrespondingAuthor = author.IsCorrespondingAuthor
                })
                .GroupBy(author => new
                {
                    Name = NormalizeAuthorFullName(author).ToUpperInvariant(),
                    Email = (author.Email ?? string.Empty).ToUpperInvariant()
                })
                .Select(group => group.Last())
                .ToList();
        }

        private static (string FirstName, string LastName) ResolveAuthorName(SubmissionAuthorInputDto authorInput)
        {
            return BackOfficeTextNormalizer.NormalizeAuthorNameParts(
                authorInput.FirstName,
                authorInput.LastName,
                authorInput.FullName);
        }

        private static string NormalizeAuthorFullName(SubmissionAuthorInputDto authorInput)
        {
            (string firstName, string lastName) = ResolveAuthorName(authorInput);
            return BackOfficeTextNormalizer.NormalizePersonFullName(firstName, lastName);
        }

        private static Guid? NormalizeOptionalGuid(Guid? value)
            => value.HasValue && value.Value != Guid.Empty ? value.Value : null;

        private static string? NormalizeOptional(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static bool IsDeleted(object entity)
        {
            object? deletedDate = entity.GetType().GetProperty("DeletedDate")?.GetValue(entity);
            return deletedDate is not null;
        }
    }
}
