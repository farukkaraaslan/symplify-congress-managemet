using Core.Application.Pipelines.Authorization;
using Symplify.BackOffice.Application.Common.Text;
using Core.Application.Pipelines.Caching;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Features.Submissions.Commands.Create;
using Symplify.BackOffice.Application.Features.ExhibitionApplications.Commands.Create;
using Symplify.BackOffice.Application.Features.Submissions.Constants;
using Symplify.BackOffice.Application.Features.Submissions.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Application.Services.Workflow;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Application.Features.Submissions.Commands.Update;

public sealed class UpdateSubmissionCommand : IRequest<UpdatedSubmissionResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }

    public Guid? SubmissionTypeId { get; set; }

    public Guid? TopicId { get; set; }

    public Guid? LanguageId { get; set; }

    public Guid? RequestedByUserId { get; set; }

    public bool RequestedByCanManageAllSubmissions { get; set; }

    public bool IsExhibitionApplication { get; set; }

    public string? Orcid { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? TitleEn { get; set; }

    public string? Abstract { get; set; }

    public string? AbstractEn { get; set; }

    public string? Keywords { get; set; }

    public string? KeywordsEn { get; set; }

    public string? WorkName { get; set; }

    public string? Dimensions { get; set; }

    public string? Technique { get; set; }

    public string? Description { get; set; }

    public string? Address { get; set; }

    public ExhibitionApplicationFileInputDto? ExhibitionFile { get; set; }

    public bool SubmitForReview { get; set; }

    public List<SubmissionAuthorInputDto> Authors { get; set; } = new();

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetSubmissions";

    public string[] Roles => new[] { SubmissionsOperationClaims.Admin, SubmissionsOperationClaims.Write, SubmissionsOperationClaims.Update };

    public sealed class UpdateSubmissionCommandHandler : IRequestHandler<UpdateSubmissionCommand, UpdatedSubmissionResponse>
    {
        private readonly ISubmissionRepository _submissionRepository;
        private readonly IAuthorRepository _authorRepository;
        private readonly ISubmissionHistoryRepository _submissionHistoryRepository;
        private readonly ICongressSubmissionTypeRepository _congressSubmissionTypeRepository;
        private readonly ICongressTopicRepository _congressTopicRepository;
        private readonly ICongressTransactionStatusTransitionRepository _congressTransactionStatusTransitionRepository;
        private readonly IAcceptanceLetterService _acceptanceLetterService;
        private readonly ISubmissionFileRepository _submissionFileRepository;
        private readonly SubmissionBusinessRules _rules;

        public UpdateSubmissionCommandHandler(
            ISubmissionRepository submissionRepository,
            IAuthorRepository authorRepository,
            ISubmissionHistoryRepository submissionHistoryRepository,
            ICongressSubmissionTypeRepository congressSubmissionTypeRepository,
            ICongressTopicRepository congressTopicRepository,
            ICongressTransactionStatusTransitionRepository congressTransactionStatusTransitionRepository,
            IAcceptanceLetterService acceptanceLetterService,
            ISubmissionFileRepository submissionFileRepository,
            SubmissionBusinessRules rules)
        {
            _submissionRepository = submissionRepository;
            _authorRepository = authorRepository;
            _submissionHistoryRepository = submissionHistoryRepository;
            _congressSubmissionTypeRepository = congressSubmissionTypeRepository;
            _congressTopicRepository = congressTopicRepository;
            _congressTransactionStatusTransitionRepository = congressTransactionStatusTransitionRepository;
            _acceptanceLetterService = acceptanceLetterService;
            _submissionFileRepository = submissionFileRepository;
            _rules = rules;
        }

        public async Task<UpdatedSubmissionResponse> Handle(UpdateSubmissionCommand request, CancellationToken cancellationToken)
        {
            Submission? entity = await _submissionRepository
                .Query()
                .Include(submission => submission.Authors)
                .Include(submission => submission.TransactionStatus)
                .Include(submission => submission.SubmissionType)
                .Include(submission => submission.ExhibitionDetail)
                .FirstOrDefaultAsync(submission => submission.Id == request.Id, cancellationToken);

            await _rules.SubmissionShouldExistWhenSelected(entity);
            entity = entity!;
            await _rules.SubmissionShouldBeAccessibleForUser(entity, request.RequestedByUserId, request.RequestedByCanManageAllSubmissions);
            await _rules.SubmissionShouldBeEditable(entity, request.RequestedByCanManageAllSubmissions);

            await EnsureSubmissionTypeIsBoundToCongressAsync(entity.CongressId, request.SubmissionTypeId, cancellationToken);
            bool isExhibitionApplication = request.IsExhibitionApplication ||
                entity.SubmissionType?.FormProfile == SubmissionFormProfile.ExhibitionApplication;

            if (!isExhibitionApplication)
                await EnsureTopicIsBoundToCongressAsync(entity.CongressId, request.TopicId, cancellationToken);

            List<SubmissionAuthorInputDto> authors = NormalizeAuthors(request.Authors);

            if (!authors.Any(author => author.IsCorrespondingAuthor))
                throw new InvalidOperationException(SubmissionsMessages.CorrespondingAuthorRequired);

            string effectiveTitle = isExhibitionApplication
                ? BackOfficeTextNormalizer.NormalizeRequiredSubmissionTitleTr(request.WorkName)
                : BackOfficeTextNormalizer.NormalizeRequiredSubmissionTitleTr(request.Title);
            string? effectiveAbstract = isExhibitionApplication
                ? NormalizeOptional(request.Description)
                : NormalizeOptional(request.Abstract);
            string? effectiveKeywords = isExhibitionApplication
                ? NormalizeOptional(request.Technique)
                : NormalizeOptional(request.Keywords);

            string previousAcceptanceLetterSnapshot = BuildAcceptanceLetterSnapshot(entity);
            string requestedAcceptanceLetterSnapshot = BuildAcceptanceLetterSnapshot(request.SubmissionTypeId, effectiveTitle, authors);
            bool acceptanceLetterContentChanged = !string.Equals(
                previousAcceptanceLetterSnapshot,
                requestedAcceptanceLetterSnapshot,
                StringComparison.Ordinal);

            DateTime now = DateTime.UtcNow;
            string auditActor = request.RequestedByUserId?.ToString() ?? "system";
            bool wasSubmitted = entity.IsSubmitted;
            int? previousStatusId = entity.TransactionStatusId;
            CongressTransactionStatusTransition? submitTransition = null;

            entity.SubmissionTypeId = request.SubmissionTypeId;
            entity.TopicId = isExhibitionApplication ? null : request.TopicId;
            entity.LanguageId = request.LanguageId;
            entity.Orcid = isExhibitionApplication ? null : NormalizeOptional(request.Orcid);
            entity.Title = effectiveTitle;
            entity.TitleEn = isExhibitionApplication ? null : BackOfficeTextNormalizer.NormalizeSubmissionTitleEn(request.TitleEn);
            entity.Abstract = effectiveAbstract;
            entity.AbstractEn = isExhibitionApplication ? null : BackOfficeTextNormalizer.NormalizeEnglishText(request.AbstractEn);
            entity.Keywords = effectiveKeywords;
            entity.KeywordsEn = isExhibitionApplication ? null : BackOfficeTextNormalizer.NormalizeEnglishText(request.KeywordsEn);
            entity.UpdatedDate = now;
            entity.UpdatedBy = auditActor;

            if (isExhibitionApplication)
            {
                ApplyExhibitionDetail(entity, request, now, auditActor);
            }

            if (request.SubmitForReview)
            {
                if (!previousStatusId.HasValue)
                    throw new InvalidOperationException(SubmissionsMessages.SubmissionSubmitTransitionNotConfigured);

                submitTransition = await GetSubmitTransitionAsync(entity.CongressId, previousStatusId.Value, cancellationToken);

                entity.TransactionStatusId = submitTransition.TransactionStatusTransition.ToStatusId;
                entity.IsSubmitted = true;
                entity.SubmittedAt ??= now;
            }

            IReadOnlyList<SubmissionAuthorInputDto> newAuthors = SynchronizeExistingAuthors(entity, authors, now, auditActor);

            Submission updatedEntity = await _submissionRepository.UpdateAsync(entity);

            if (isExhibitionApplication)
                await ReplaceExhibitionFileAsync(updatedEntity.Id, request.ExhibitionFile, now, auditActor, cancellationToken);

            foreach (SubmissionAuthorInputDto authorInput in newAuthors)
            {
                Author author = CreateAuthor(authorInput, now, auditActor);
                author.Submissions.Add(updatedEntity);
                await _authorRepository.AddAsync(author);
            }

            await _submissionHistoryRepository.AddAsync(new SubmissionHistory
            {
                Id = Guid.NewGuid(),
                SubmissionId = updatedEntity.Id,
                FromStatusId = previousStatusId,
                ToStatusId = updatedEntity.TransactionStatusId,
                TransactionStatusTransitionId = submitTransition?.TransactionStatusTransitionId,
                PerformedByUserId = request.RequestedByUserId,
                Note = request.SubmitForReview && !wasSubmitted
                    ? SubmissionsMessages.SubmissionUpdatedAndSubmittedHistoryNote
                    : SubmissionsMessages.SubmissionUpdatedHistoryNote,
                PerformedAt = now,
                IsAutomatic = false,
                CreatedDate = now,
                CreatedBy = auditActor
            });

            if (IsAcceptedStatus(updatedEntity.TransactionStatus?.Code))
            {
                bool hasMissingAcceptanceLetters = !acceptanceLetterContentChanged &&
                    await _acceptanceLetterService.HasMissingCurrentLettersAsync(updatedEntity, cancellationToken);

                if (acceptanceLetterContentChanged || hasMissingAcceptanceLetters)
                    await _acceptanceLetterService.ReplaceCurrentAsync(updatedEntity, request.RequestedByUserId, cancellationToken);
            }

            return new UpdatedSubmissionResponse
            {
                Id = updatedEntity.Id,
                CongressId = updatedEntity.CongressId,
                SubmissionTypeId = updatedEntity.SubmissionTypeId,
                TopicId = updatedEntity.TopicId,
                CreatedByUserId = updatedEntity.CreatedByUserId,
                LanguageId = updatedEntity.LanguageId,
                PaymentStatusId = updatedEntity.PaymentStatusId,
                TransactionStatusId = updatedEntity.TransactionStatusId,
                SubmissionNumber = updatedEntity.SubmissionNumber,
                Orcid = updatedEntity.Orcid,
                Title = updatedEntity.Title,
                TitleEn = updatedEntity.TitleEn,
                Abstract = updatedEntity.Abstract,
                AbstractEn = updatedEntity.AbstractEn,
                Keywords = updatedEntity.Keywords,
                KeywordsEn = updatedEntity.KeywordsEn,
                SubmittedAt = updatedEntity.SubmittedAt
            };
        }

        private static void ApplyExhibitionDetail(Submission entity, UpdateSubmissionCommand request, DateTime now, string auditActor)
        {
            entity.ExhibitionDetail ??= new SubmissionExhibitionDetail
            {
                Id = Guid.NewGuid(),
                SubmissionId = entity.Id,
                CreatedDate = now,
                CreatedBy = auditActor
            };

            entity.ExhibitionDetail.WorkName = BackOfficeTextNormalizer.NormalizeRequiredSubmissionTitleTr(request.WorkName);
            entity.ExhibitionDetail.Dimensions = NormalizeOptional(request.Dimensions);
            entity.ExhibitionDetail.Technique = (request.Technique ?? string.Empty).Trim();
            entity.ExhibitionDetail.Description = NormalizeOptional(request.Description);
            entity.ExhibitionDetail.Address = (request.Address ?? string.Empty).Trim();
            entity.ExhibitionDetail.DeletedDate = null;
            entity.ExhibitionDetail.DeletedBy = null;
            entity.ExhibitionDetail.UpdatedDate = now;
            entity.ExhibitionDetail.UpdatedBy = auditActor;
        }

        private async Task ReplaceExhibitionFileAsync(
            Guid submissionId,
            ExhibitionApplicationFileInputDto? file,
            DateTime now,
            string auditActor,
            CancellationToken cancellationToken)
        {
            if (file is null || string.IsNullOrWhiteSpace(file.FilePath))
                return;

            List<SubmissionFile> activeFiles = await _submissionFileRepository
                .Query()
                .Where(item =>
                    item.SubmissionId == submissionId &&
                    item.FileKind == SubmissionFileKind.ExhibitionImage &&
                    item.DeletedDate == null &&
                    item.IsActive)
                .ToListAsync(cancellationToken);

            foreach (SubmissionFile existingFile in activeFiles)
            {
                existingFile.IsActive = false;
                existingFile.UpdatedDate = now;
                existingFile.UpdatedBy = auditActor;

                await _submissionFileRepository.UpdateAsync(existingFile);
            }

            await _submissionFileRepository.AddAsync(new SubmissionFile
            {
                Id = Guid.NewGuid(),
                SubmissionId = submissionId,
                FileKind = SubmissionFileKind.ExhibitionImage,
                OriginalFileName = file.OriginalFileName.Trim(),
                FilePath = file.FilePath.Trim(),
                ContentType = NormalizeOptional(file.ContentType),
                FileSize = file.FileSize,
                IsActive = true,
                CreatedDate = now,
                CreatedBy = auditActor
            });
        }

        private async Task<CongressTransactionStatusTransition> GetSubmitTransitionAsync(
            Guid congressId,
            int currentStatusId,
            CancellationToken cancellationToken)
        {
            CongressTransactionStatusTransition? transition = await _congressTransactionStatusTransitionRepository
                .Query()
                .Include(entity => entity.TransactionStatusTransition)
                .Where(entity =>
                    entity.CongressId == congressId &&
                    entity.IsActive &&
                    entity.DeletedDate == null &&
                    entity.TransactionStatusTransition.IsActive &&
                    !entity.TransactionStatusTransition.IsAuto &&
                    entity.TransactionStatusTransition.DeletedDate == null &&
                    entity.TransactionStatusTransition.FromStatusId == currentStatusId)
                .OrderBy(entity => entity.Order)
                .ThenBy(entity => entity.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (transition is null)
                throw new InvalidOperationException(SubmissionsMessages.SubmissionSubmitTransitionNotConfigured);

            return transition;
        }

        private async Task EnsureSubmissionTypeIsBoundToCongressAsync(Guid congressId, Guid? submissionTypeId, CancellationToken cancellationToken)
        {
            if (!submissionTypeId.HasValue || submissionTypeId.Value == Guid.Empty)
                throw new InvalidOperationException(SubmissionsMessages.SubmissionTypeRequired);

            bool exists = await _congressSubmissionTypeRepository
                .Query()
                .AnyAsync(entity =>
                    entity.CongressId == congressId &&
                    entity.SubmissionTypeId == submissionTypeId.Value &&
                    entity.IsActive &&
                    entity.DeletedDate == null,
                    cancellationToken);

            if (!exists)
                throw new InvalidOperationException(SubmissionsMessages.SubmissionTypeNotAvailableForCongress);
        }

        private async Task EnsureTopicIsBoundToCongressAsync(Guid congressId, Guid? topicId, CancellationToken cancellationToken)
        {
            if (!topicId.HasValue || topicId.Value == Guid.Empty)
                throw new InvalidOperationException(SubmissionsMessages.TopicRequired);

            bool exists = await _congressTopicRepository
                .Query()
                .AnyAsync(entity =>
                    entity.CongressId == congressId &&
                    entity.TopicId == topicId.Value &&
                    entity.IsActive &&
                    entity.DeletedDate == null,
                    cancellationToken);

            if (!exists)
                throw new InvalidOperationException(SubmissionsMessages.TopicNotAvailableForCongress);
        }

        private static IReadOnlyList<SubmissionAuthorInputDto> SynchronizeExistingAuthors(
            Submission submission,
            IReadOnlyCollection<SubmissionAuthorInputDto> authors,
            DateTime now,
            string auditActor)
        {
            Dictionary<Guid, Author> existingAuthors = submission.Authors
                .Where(author => author.Id != Guid.Empty)
                .ToDictionary(author => author.Id, author => author);

            HashSet<Guid> requestedExistingAuthorIds = authors
                .Where(author => author.Id.HasValue && author.Id.Value != Guid.Empty)
                .Select(author => author.Id!.Value)
                .ToHashSet();

            foreach (SubmissionAuthorInputDto authorInput in authors.Where(author => author.Id.HasValue && author.Id.Value != Guid.Empty))
            {
                if (!existingAuthors.TryGetValue(authorInput.Id!.Value, out Author? author))
                    continue;

                ApplyAuthorValues(author, authorInput, now, auditActor);
            }

            List<Author> authorsToDetach = submission.Authors
                .Where(author => author.Id != Guid.Empty && !requestedExistingAuthorIds.Contains(author.Id))
                .ToList();

            foreach (Author author in authorsToDetach)
            {
                submission.Authors.Remove(author);
                author.UpdatedDate = now;
                author.UpdatedBy = auditActor;
            }

            return authors
                .Where(author => !author.Id.HasValue || author.Id.Value == Guid.Empty || !existingAuthors.ContainsKey(author.Id.Value))
                .ToList();
        }

        private static Author CreateAuthor(SubmissionAuthorInputDto authorInput, DateTime now, string auditActor)
        {
            Author author = new()
            {
                Id = Guid.NewGuid(),
                CreatedDate = now,
                CreatedBy = auditActor
            };

            ApplyAuthorValues(author, authorInput, now, auditActor);
            return author;
        }

        private static void ApplyAuthorValues(Author author, SubmissionAuthorInputDto authorInput, DateTime now, string auditActor)
        {
            (string firstName, string lastName) = ResolveAuthorName(authorInput);
            author.FirstName = firstName;
            author.LastName = lastName;
            author.TitleId = NormalizeOptionalGuid(authorInput.TitleId);
            author.Email = NormalizeOptional(authorInput.Email);
            author.Institution = BackOfficeTextNormalizer.NormalizeInstitution(authorInput.Institution);
            author.Orcid = NormalizeOptional(authorInput.Orcid);
            author.IsCorrespondingAuthor = authorInput.IsCorrespondingAuthor;
            author.DeletedDate = null;

            if (author.CreatedDate == default)
                author.CreatedDate = now;

            if (string.IsNullOrWhiteSpace(author.CreatedBy))
                author.CreatedBy = auditActor;

            author.UpdatedDate = now;
            author.UpdatedBy = auditActor;
        }

        private static string BuildAcceptanceLetterSnapshot(Submission submission)
        {
            IEnumerable<SubmissionAuthorInputDto> authors = submission.Authors
                .Where(author => author.DeletedDate == null)
                .Select(author => new SubmissionAuthorInputDto
                {
                    Id = author.Id,
                    TitleId = author.TitleId,
                    FirstName = author.FirstName,
                    LastName = author.LastName,
                    FullName = JoinName(author.FirstName, author.LastName),
                    Email = author.Email,
                    Institution = author.Institution,
                    Orcid = author.Orcid,
                    IsCorrespondingAuthor = author.IsCorrespondingAuthor
                });

            return BuildAcceptanceLetterSnapshot(submission.SubmissionTypeId, submission.Title, authors);
        }

        private static string BuildAcceptanceLetterSnapshot(
            Guid? submissionTypeId,
            string? title,
            IEnumerable<SubmissionAuthorInputDto> authors)
        {
            IEnumerable<string> authorParts = authors
                .Select(author => new
                {
                    author.Id,
                    author.TitleId,
                    FullName = NormalizeForSnapshot(NormalizeAuthorFullName(author)),
                    Email = NormalizeForSnapshot(author.Email),
                    Institution = NormalizeForSnapshot(author.Institution),
                    Orcid = NormalizeForSnapshot(author.Orcid),
                    author.IsCorrespondingAuthor
                })
                .OrderByDescending(author => author.IsCorrespondingAuthor)
                .ThenBy(author => author.Id ?? Guid.Empty)
                .ThenBy(author => author.FullName, StringComparer.Ordinal)
                .ThenBy(author => author.Email, StringComparer.Ordinal)
                .Select(author => string.Join('|',
                    author.Id?.ToString("D") ?? string.Empty,
                    author.TitleId?.ToString("D") ?? string.Empty,
                    author.FullName,
                    author.Email,
                    author.Institution,
                    author.Orcid,
                    author.IsCorrespondingAuthor ? "1" : "0"));

            return string.Join("||",
                submissionTypeId?.ToString("D") ?? string.Empty,
                NormalizeForSnapshot(title),
                string.Join(";;", authorParts));
        }

        private static bool IsAcceptedStatus(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            string normalized = new string(code.Trim().Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
            return normalized == "ACCEPTED" || normalized == "KABULEDILDI";
        }

        private static string JoinName(string? firstName, string? lastName)
            => $"{NormalizeOptional(firstName)} {NormalizeOptional(lastName)}".Trim();

        private static string NormalizeForSnapshot(string? value)
            => string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().Normalize().ToUpperInvariant();

        private static List<SubmissionAuthorInputDto> NormalizeAuthors(IEnumerable<SubmissionAuthorInputDto>? authors)
        {
            if (authors is null)
                return new List<SubmissionAuthorInputDto>();

            return authors
                .Where(author => !string.IsNullOrWhiteSpace(author.FirstName)
                    || !string.IsNullOrWhiteSpace(author.LastName)
                    || !string.IsNullOrWhiteSpace(author.FullName))
                .Select(author => new SubmissionAuthorInputDto
                {
                    Id = author.Id,
                    TitleId = NormalizeOptionalGuid(author.TitleId),
                    FirstName = BackOfficeTextNormalizer.NormalizePersonFirstName(author.FirstName),
                    LastName = BackOfficeTextNormalizer.NormalizePersonSurname(author.LastName),
                    FullName = NormalizeAuthorFullName(author),
                    Email = NormalizeOptional(author.Email),
                    Institution = BackOfficeTextNormalizer.NormalizeInstitution(author.Institution),
                    Orcid = NormalizeOptional(author.Orcid),
                    IsCorrespondingAuthor = author.IsCorrespondingAuthor
                })
                .GroupBy(author => author.Id.HasValue && author.Id.Value != Guid.Empty
                    ? $"id:{author.Id.Value}"
                    : $"new:{NormalizeAuthorFullName(author).ToUpperInvariant()}:{(author.Email ?? string.Empty).ToUpperInvariant()}")
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
        {
            return value.HasValue && value.Value != Guid.Empty ? value.Value : null;
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
