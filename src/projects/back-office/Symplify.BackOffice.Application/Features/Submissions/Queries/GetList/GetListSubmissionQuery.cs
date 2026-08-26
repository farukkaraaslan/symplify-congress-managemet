using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.Submissions.Constants;
using Symplify.BackOffice.Application.Features.Submissions.Rules;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Lookups;
using Symplify.BackOffice.Domain.Submission;
using Symplify.BackOffice.Domain.Workflow;
using CongressStatus = Symplify.BackOffice.Domain.Enums.CongressStatus;

namespace Symplify.BackOffice.Application.Features.Submissions.Queries.GetList;

public sealed class GetListSubmissionQuery : IRequest<GetListResponse<GetListSubmissionListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();

    public Guid? CreatedByUserId { get; set; }

    public Guid? RequestedByUserId { get; set; }

    public bool CanManageAllSubmissions { get; set; }

    public Guid? CongressId { get; set; }

    /// <summary>
    /// false: yalnızca Published kongrelerin bildirileri.
    /// true: yalnızca Archived kongrelerin bildirileri.
    /// </summary>
    public bool ArchiveMode { get; set; }

    public Guid? SubmissionTypeId { get; set; }

    public Guid? TopicId { get; set; }

    public int? TransactionStatusId { get; set; }

    public int? PaymentStatusId { get; set; }

    public SubmissionOwnerMultiplicityFilter OwnerMultiplicity { get; set; } = SubmissionOwnerMultiplicityFilter.All;

    public Guid? LanguageId { get; set; }

    public string? Culture { get; set; }

    public string? SearchText { get; set; }

    public string? SortColumn { get; set; }

    public string? SortDirection { get; set; }

    public string[] Roles => new[] { SubmissionsOperationClaims.Admin, SubmissionsOperationClaims.Read };

    public bool BypassCache => true;

    public string CacheKey => $"GetListSubmissions({PageRequest.Page},{PageRequest.PageSize},{CreatedByUserId},{RequestedByUserId},{CanManageAllSubmissions},{CongressId},{ArchiveMode},{SubmissionTypeId},{TopicId},{TransactionStatusId},{PaymentStatusId},{OwnerMultiplicity},{LanguageId},{Culture},{SearchText},{SortColumn},{SortDirection})";

    public string CacheGroupKey => "GetSubmissions";

    public TimeSpan? SlidingExpiration { get; }

    public sealed class GetListSubmissionQueryHandler : IRequestHandler<GetListSubmissionQuery, GetListResponse<GetListSubmissionListItemDto>>
    {
        private readonly ISubmissionRepository _repository;
        private readonly ICongressRepository _congressRepository;
        private readonly ICongressTranslationRepository _congressTranslationRepository;
        private readonly ISubmissionTypeRepository _submissionTypeRepository;
        private readonly ISubmissionTypeTranslationRepository _submissionTypeTranslationRepository;
        private readonly ITopicRepository _topicRepository;
        private readonly ITopicTranslationRepository _topicTranslationRepository;
        private readonly IPaymentStatusRepository _paymentStatusRepository;
        private readonly IPaymentStatusTranslationRepository _paymentStatusTranslationRepository;
        private readonly ITransactionStatusRepository _transactionStatusRepository;
        private readonly ITransactionStatusTranslationRepository _transactionStatusTranslationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetListSubmissionQueryHandler(
            ISubmissionRepository repository,
            ICongressRepository congressRepository,
            ICongressTranslationRepository congressTranslationRepository,
            ISubmissionTypeRepository submissionTypeRepository,
            ISubmissionTypeTranslationRepository submissionTypeTranslationRepository,
            ITopicRepository topicRepository,
            ITopicTranslationRepository topicTranslationRepository,
            IPaymentStatusRepository paymentStatusRepository,
            IPaymentStatusTranslationRepository paymentStatusTranslationRepository,
            ITransactionStatusRepository transactionStatusRepository,
            ITransactionStatusTranslationRepository transactionStatusTranslationRepository,
            IApplicationLanguageProvider languageProvider,
            ICurrentLanguageProvider currentLanguageProvider,
            ITranslationFallbackResolver fallbackResolver)
        {
            _repository = repository;
            _congressRepository = congressRepository;
            _congressTranslationRepository = congressTranslationRepository;
            _submissionTypeRepository = submissionTypeRepository;
            _submissionTypeTranslationRepository = submissionTypeTranslationRepository;
            _topicRepository = topicRepository;
            _topicTranslationRepository = topicTranslationRepository;
            _paymentStatusRepository = paymentStatusRepository;
            _paymentStatusTranslationRepository = paymentStatusTranslationRepository;
            _transactionStatusRepository = transactionStatusRepository;
            _transactionStatusTranslationRepository = transactionStatusTranslationRepository;
            _languageProvider = languageProvider;
            _currentLanguageProvider = currentLanguageProvider;
            _fallbackResolver = fallbackResolver;
        }

        public async Task<GetListResponse<GetListSubmissionListItemDto>> Handle(GetListSubmissionQuery request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(request.LanguageId, request.Culture, defaultLanguage, cancellationToken);

            List<Submission> roots = _repository
                .Query()
                .Include(submission => submission.CreatedByUser)
                .Include(submission => submission.Authors)
                    .ThenInclude(author => author.Title)
                        .ThenInclude(title => title!.Translations)
                .ToList()
                .Where(submission => !IsDeleted(submission))
                .ToList();

            List<Congress> congresses = _congressRepository
                .Query()
                .ToList()
                .Where(item => !IsDeleted(item))
                .ToList();

            CongressStatus targetCongressStatus = request.ArchiveMode
                ? CongressStatus.Archived
                : CongressStatus.Published;

            HashSet<Guid> scopedCongressIds = congresses
                .Where(congress => congress.Status == targetCongressStatus)
                .Select(congress => congress.Id)
                .ToHashSet();

            roots = roots
                .Where(submission => scopedCongressIds.Contains(submission.CongressId))
                .ToList();

            Dictionary<(Guid CongressId, Guid CreatedByUserId), int> ownerSubmissionCounts = roots
                .Where(submission => submission.CreatedByUserId.HasValue && submission.CreatedByUserId.Value != Guid.Empty)
                .GroupBy(submission => (submission.CongressId, submission.CreatedByUserId!.Value))
                .ToDictionary(group => group.Key, group => group.Count());

            if (request.CreatedByUserId.HasValue && request.CreatedByUserId.Value != Guid.Empty)
                roots = roots.Where(submission => submission.CreatedByUserId == request.CreatedByUserId.Value).ToList();

            if (request.CongressId.HasValue && request.CongressId.Value != Guid.Empty)
                roots = roots.Where(submission => submission.CongressId == request.CongressId.Value).ToList();

            if (request.SubmissionTypeId.HasValue && request.SubmissionTypeId.Value != Guid.Empty)
                roots = roots.Where(submission => submission.SubmissionTypeId == request.SubmissionTypeId.Value).ToList();

            if (request.TopicId.HasValue && request.TopicId.Value != Guid.Empty)
                roots = roots.Where(submission => submission.TopicId == request.TopicId.Value).ToList();

            if (request.TransactionStatusId.HasValue && request.TransactionStatusId.Value > 0)
                roots = roots.Where(submission => submission.TransactionStatusId == request.TransactionStatusId.Value).ToList();

            if (request.PaymentStatusId.HasValue && request.PaymentStatusId.Value > 0)
                roots = roots.Where(submission => submission.PaymentStatusId == request.PaymentStatusId.Value).ToList();


            List<CongressTranslation> congressTranslations = _congressTranslationRepository.Query().ToList().Where(item => !IsDeleted(item)).ToList();
            List<SubmissionType> submissionTypes = _submissionTypeRepository.Query().ToList().Where(item => !IsDeleted(item)).ToList();
            List<SubmissionTypeTranslation> submissionTypeTranslations = _submissionTypeTranslationRepository.Query().ToList().Where(item => !IsDeleted(item)).ToList();
            List<Topic> topics = _topicRepository.Query().ToList().Where(item => !IsDeleted(item)).ToList();
            List<TopicTranslation> topicTranslations = _topicTranslationRepository.Query().ToList().Where(item => !IsDeleted(item)).ToList();
            List<PaymentStatus> paymentStatuses = _paymentStatusRepository.Query().ToList().Where(item => !IsDeleted(item)).ToList();
            List<PaymentStatusTranslation> paymentStatusTranslations = _paymentStatusTranslationRepository.Query().ToList().Where(item => !IsDeleted(item)).ToList();
            List<TransactionStatus> transactionStatuses = _transactionStatusRepository.Query().ToList().Where(item => !IsDeleted(item)).ToList();
            List<TransactionStatusTranslation> transactionStatusTranslations = _transactionStatusTranslationRepository.Query().ToList().Where(item => !IsDeleted(item)).ToList();

            List<GetListSubmissionListItemDto> projected = roots.Select(submission => Project(
                    submission,
                    congresses,
                    congressTranslations,
                    submissionTypes,
                    submissionTypeTranslations,
                    topics,
                    topicTranslations,
                    paymentStatuses,
                    paymentStatusTranslations,
                    transactionStatuses,
                    transactionStatusTranslations,
                    requestedLanguage.Id,
                    defaultLanguage.Id,
                    request.RequestedByUserId,
                    request.CanManageAllSubmissions,
                    ResolveOwnerSubmissionCount(submission, ownerSubmissionCounts)))
                .ToList();

            projected = request.OwnerMultiplicity switch
            {
                SubmissionOwnerMultiplicityFilter.Single => projected
                    .Where(item => item.OwnerSubmissionCount == 1)
                    .ToList(),

                SubmissionOwnerMultiplicityFilter.Multiple => projected
                    .Where(item => item.OwnerSubmissionCount > 1)
                    .ToList(),

                _ => projected
            };

            if (!string.IsNullOrWhiteSpace(request.SearchText))
            {
                string search = request.SearchText.Trim();
                projected = projected.Where(item =>
                        Contains(item.SubmissionNumber, search) ||
                        Contains(item.Title, search) ||
                        Contains(item.TitleEn, search) ||
                        Contains(item.TopicName, search) ||
                        Contains(item.SubmissionTypeName, search) ||
                        Contains(item.CongressName, search) ||
                        Contains(item.CongressCode, search) ||
                        Contains(item.SubmissionOwnerName, search) ||
                        Contains(item.SubmissionOwnerEmail, search) ||
                        Contains(item.CorrespondingAuthorName, search))
                    .ToList();
            }

            projected = ApplySorting(projected, request.SortColumn, request.SortDirection);

            int page = request.PageRequest.Page < 0 ? 0 : request.PageRequest.Page;
            int pageSize = request.PageRequest.PageSize <= 0 ? 50 : request.PageRequest.PageSize;
            int total = projected.Count;
            int pages = (int)Math.Ceiling(total / (double)pageSize);
            List<GetListSubmissionListItemDto> items = projected.Skip(page * pageSize).Take(pageSize).ToList();

            return new GetListResponse<GetListSubmissionListItemDto>
            {
                Index = page,
                Size = pageSize,
                Count = total,
                Pages = pages,
                HasPrevious = page > 0,
                HasNext = page + 1 < pages,
                Items = items
            };
        }

        private static List<GetListSubmissionListItemDto> ApplySorting(
            List<GetListSubmissionListItemDto> items,
            string? sortColumn,
            string? sortDirection)
        {
            bool descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            string normalizedColumn = NormalizeSortColumn(sortColumn);

            IOrderedEnumerable<GetListSubmissionListItemDto> ordered = normalizedColumn switch
            {
                "submission" or "submissionnumber" => descending
                    ? items.OrderByDescending(item => NormalizeSortValue(item.SubmissionNumber))
                        .ThenByDescending(item => ResolveDisplayDate(item))
                    : items.OrderBy(item => NormalizeSortValue(item.SubmissionNumber))
                        .ThenByDescending(item => ResolveDisplayDate(item)),

                "title" => descending
                    ? items.OrderByDescending(item => NormalizeSortValue(item.Title))
                        .ThenByDescending(item => ResolveDisplayDate(item))
                    : items.OrderBy(item => NormalizeSortValue(item.Title))
                        .ThenByDescending(item => ResolveDisplayDate(item)),

                "congress" => descending
                    ? items.OrderByDescending(item => NormalizeSortValue(item.CongressName))
                        .ThenByDescending(item => ResolveDisplayDate(item))
                    : items.OrderBy(item => NormalizeSortValue(item.CongressName))
                        .ThenByDescending(item => ResolveDisplayDate(item)),

                "typetopic" => descending
                    ? items.OrderByDescending(item => NormalizeSortValue(item.SubmissionTypeName))
                        .ThenByDescending(item => NormalizeSortValue(item.TopicName))
                        .ThenByDescending(item => ResolveDisplayDate(item))
                    : items.OrderBy(item => NormalizeSortValue(item.SubmissionTypeName))
                        .ThenBy(item => NormalizeSortValue(item.TopicName))
                        .ThenByDescending(item => ResolveDisplayDate(item)),

                "owner" => descending
                    ? items.OrderByDescending(item => item.OwnerSubmissionCount)
                        .ThenByDescending(item => NormalizeSortValue(item.SubmissionOwnerName))
                        .ThenByDescending(item => ResolveDisplayDate(item))
                    : items.OrderBy(item => item.OwnerSubmissionCount)
                        .ThenBy(item => NormalizeSortValue(item.SubmissionOwnerName))
                        .ThenByDescending(item => ResolveDisplayDate(item)),

                "authors" => descending
                    ? items.OrderByDescending(item => NormalizeSortValue(item.CorrespondingAuthorName))
                        .ThenByDescending(item => ResolveDisplayDate(item))
                    : items.OrderBy(item => NormalizeSortValue(item.CorrespondingAuthorName))
                        .ThenByDescending(item => ResolveDisplayDate(item)),

                "payment" => descending
                    ? items.OrderByDescending(item => NormalizeSortValue(item.PaymentStatusName))
                        .ThenByDescending(item => ResolveDisplayDate(item))
                    : items.OrderBy(item => NormalizeSortValue(item.PaymentStatusName))
                        .ThenByDescending(item => ResolveDisplayDate(item)),

                "status" => descending
                    ? items.OrderByDescending(item => NormalizeSortValue(item.TransactionStatusName))
                        .ThenByDescending(item => ResolveDisplayDate(item))
                    : items.OrderBy(item => NormalizeSortValue(item.TransactionStatusName))
                        .ThenByDescending(item => ResolveDisplayDate(item)),

                "submittedat" or "date" => descending
                    ? items.OrderByDescending(ResolveDisplayDate)
                        .ThenByDescending(item => item.CreatedDate)
                    : items.OrderBy(ResolveDisplayDate)
                        .ThenBy(item => item.CreatedDate),

                _ => items.OrderByDescending(ResolveDisplayDate)
                    .ThenByDescending(item => item.CreatedDate)
            };

            return ordered.ToList();
        }

        private static string NormalizeSortColumn(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "submittedat";

            return new string(value.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        }

        private static string NormalizeSortValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }

        private static DateTime ResolveDisplayDate(GetListSubmissionListItemDto item)
        {
            return item.SubmittedAt ?? item.UpdatedDate ?? item.CreatedDate;
        }

        private async Task<ApplicationLanguageDto> ResolveRequestedLanguageAsync(Guid? languageId, string? culture, ApplicationLanguageDto defaultLanguage, CancellationToken cancellationToken)
        {
            if (languageId.HasValue && languageId.Value != Guid.Empty)
                return await _languageProvider.GetByIdAsync(languageId.Value, cancellationToken) ?? defaultLanguage;

            if (!string.IsNullOrWhiteSpace(culture))
                return await _languageProvider.GetByCultureAsync(culture, cancellationToken) ?? defaultLanguage;

            return await _currentLanguageProvider.GetCurrentLanguageAsync(cancellationToken);
        }

        private GetListSubmissionListItemDto Project(
            Submission submission,
            List<Congress> congresses,
            List<CongressTranslation> congressTranslations,
            List<SubmissionType> submissionTypes,
            List<SubmissionTypeTranslation> submissionTypeTranslations,
            List<Topic> topics,
            List<TopicTranslation> topicTranslations,
            List<PaymentStatus> paymentStatuses,
            List<PaymentStatusTranslation> paymentStatusTranslations,
            List<TransactionStatus> transactionStatuses,
            List<TransactionStatusTranslation> transactionStatusTranslations,
            Guid requestedLanguageId,
            Guid defaultLanguageId,
            Guid? requestedByUserId,
            bool canManageAllSubmissions,
            int ownerSubmissionCount)
        {
            Congress? congress = congresses.FirstOrDefault(item => item.Id == submission.CongressId);

            SubmissionType? submissionType = submission.SubmissionTypeId.HasValue
                ? submissionTypes.FirstOrDefault(item => item.Id == submission.SubmissionTypeId.Value)
                : null;

            Topic? topic = submission.TopicId.HasValue
                ? topics.FirstOrDefault(item => item.Id == submission.TopicId.Value)
                : null;

            PaymentStatus? paymentStatus = submission.PaymentStatusId.HasValue
                ? paymentStatuses.FirstOrDefault(item => item.Id == submission.PaymentStatusId.Value)
                : null;

            TransactionStatus? transactionStatus = submission.TransactionStatusId.HasValue
                ? transactionStatuses.FirstOrDefault(item => item.Id == submission.TransactionStatusId.Value)
                : null;

            List<SubmissionTypeTranslation> rootSubmissionTypeTranslations = submissionType is null
                ? new List<SubmissionTypeTranslation>()
                : submissionTypeTranslations.Where(translation => translation.SubmissionTypeId == submissionType.Id).ToList();

            List<TopicTranslation> rootTopicTranslations = topic is null
                ? new List<TopicTranslation>()
                : topicTranslations.Where(translation => translation.TopicId == topic.Id).ToList();

            List<PaymentStatusTranslation> rootPaymentStatusTranslations = paymentStatus is null
                ? new List<PaymentStatusTranslation>()
                : paymentStatusTranslations.Where(translation => translation.PaymentStatusId == paymentStatus.Id).ToList();

            List<TransactionStatusTranslation> rootTransactionStatusTranslations = transactionStatus is null
                ? new List<TransactionStatusTranslation>()
                : transactionStatusTranslations.Where(translation => translation.TransactionStatusId == transactionStatus.Id).ToList();

            string congressName = ResolveCongressName(congress, congressTranslations, requestedLanguageId, defaultLanguageId);
            string submissionTypeName = ResolveName(rootSubmissionTypeTranslations, requestedLanguageId, defaultLanguageId) ?? submissionType?.Code ?? "-";
            string topicName = ResolveName(rootTopicTranslations, requestedLanguageId, defaultLanguageId) ?? topic?.Code ?? "-";
            string paymentStatusName = ResolveName(rootPaymentStatusTranslations, requestedLanguageId, defaultLanguageId) ?? paymentStatus?.Code ?? "-";
            string transactionStatusName = ResolveName(rootTransactionStatusTranslations, requestedLanguageId, defaultLanguageId)
                ?? transactionStatus?.Code
                ?? (submission.IsSubmitted ? "Onaya Gönderildi" : "Taslak");

            List<Author> activeAuthors = submission.Authors.Where(author => !IsDeleted(author)).ToList();
            Author? correspondingAuthor = activeAuthors.FirstOrDefault(author => author.IsCorrespondingAuthor) ?? activeAuthors.FirstOrDefault();
            List<Author> otherAuthors = activeAuthors.Where(author => correspondingAuthor is null || author.Id != correspondingAuthor.Id).ToList();

            string? submissionOwnerName = submission.CreatedByUser is null
                ? null
                : string.Join(
                    " ",
                    new[] { submission.CreatedByUser.Name, submission.CreatedByUser.Surname }
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Select(value => value.Trim()));

            bool isOwner = requestedByUserId.HasValue && requestedByUserId.Value != Guid.Empty && submission.CreatedByUserId == requestedByUserId.Value;
            bool canEdit = canManageAllSubmissions || (isOwner && SubmissionBusinessRules.IsEditableByAuthor(transactionStatus, submission.IsSubmitted));

            return new GetListSubmissionListItemDto
            {
                Id = submission.Id,
                CongressId = submission.CongressId,
                CongressCode = congress?.Code,
                CongressName = congressName,
                SubmissionTypeId = submission.SubmissionTypeId,
                TopicId = submission.TopicId,
                CreatedByUserId = submission.CreatedByUserId,
                SubmissionOwnerName = string.IsNullOrWhiteSpace(submissionOwnerName)
                    ? submission.CreatedByUser?.Email
                    : submissionOwnerName,
                SubmissionOwnerEmail = submission.CreatedByUser?.Email,
                OwnerSubmissionCount = ownerSubmissionCount,
                HasMultipleSubmissions = ownerSubmissionCount > 1,
                LanguageId = submission.LanguageId,
                PaymentStatusId = submission.PaymentStatusId,
                PaymentStatusCode = paymentStatus?.Code,
                TransactionStatusId = submission.TransactionStatusId,
                TransactionStatusCode = transactionStatus?.Code,
                SubmissionNumber = submission.SubmissionNumber,
                Orcid = submission.Orcid,
                Title = submission.Title,
                TitleEn = submission.TitleEn,
                Abstract = submission.Abstract,
                AbstractEn = submission.AbstractEn,
                Keywords = submission.Keywords,
                KeywordsEn = submission.KeywordsEn,
                IsSubmitted = submission.IsSubmitted,
                SubmittedAt = submission.SubmittedAt,
                CreatedDate = submission.CreatedDate,
                UpdatedDate = submission.UpdatedDate,
                SubmissionTypeName = submissionTypeName,
                TopicName = topicName,
                PaymentStatusName = paymentStatusName,
                TransactionStatusName = transactionStatusName,
                PaymentStatusBadgeClass = ResolvePaymentBadgeClass(paymentStatus, paymentStatusName),
                TransactionStatusBadgeClass = ResolveTransactionBadgeClass(transactionStatus, submission.IsSubmitted),
                CorrespondingAuthorName = correspondingAuthor is null ? null : GetAuthorDisplayName(correspondingAuthor, requestedLanguageId, defaultLanguageId),
                OtherAuthorsText = BuildOtherAuthorsText(otherAuthors, requestedLanguageId, defaultLanguageId),
                AuthorCount = activeAuthors.Count,
                CanEdit = canEdit,
                CanDelete = canEdit
            };
        }


        private static int ResolveOwnerSubmissionCount(
            Submission submission,
            IReadOnlyDictionary<(Guid CongressId, Guid CreatedByUserId), int> ownerSubmissionCounts)
        {
            if (!submission.CreatedByUserId.HasValue || submission.CreatedByUserId.Value == Guid.Empty)
                return 0;

            return ownerSubmissionCounts.TryGetValue(
                (submission.CongressId, submission.CreatedByUserId.Value),
                out int count)
                ? count
                : 0;
        }

        private string ResolveCongressName(Congress? congress, IEnumerable<CongressTranslation> translations, Guid requestedLanguageId, Guid defaultLanguageId)
        {
            if (congress is null)
                return "-";

            CongressTranslation? displayTranslation = _fallbackResolver.Resolve(
                translations.Where(translation => translation.CongressId == congress.Id),
                requestedLanguageId,
                defaultLanguageId);

            if (!string.IsNullOrWhiteSpace(displayTranslation?.Title))
                return displayTranslation.Title;

            return !string.IsNullOrWhiteSpace(congress.Name) ? congress.Name : congress.Code;
        }

        private string? ResolveName<TTranslation>(IEnumerable<TTranslation> translations, Guid requestedLanguageId, Guid defaultLanguageId)
            where TTranslation : class
        {
            TTranslation? displayTranslation = _fallbackResolver.Resolve(translations, requestedLanguageId, defaultLanguageId);
            object? value = displayTranslation?.GetType().GetProperty("Name")?.GetValue(displayTranslation);
            return value?.ToString();
        }

        private static string ResolvePaymentBadgeClass(PaymentStatus? paymentStatus, string displayName)
        {
            if (paymentStatus is null)
                return "bg-neutral-200 text-neutral-700";

            string value = string.Concat(paymentStatus.Code, " ", displayName).ToLowerInvariant();

            if (value.Contains("paid") || value.Contains("approved") || value.Contains("alındı") || value.Contains("onay"))
                return "bg-success-100 text-success-600";

            if (value.Contains("pending") || value.Contains("bekle"))
                return "bg-warning-100 text-warning-600";

            return "bg-primary-50 text-primary-600";
        }

        private static string ResolveTransactionBadgeClass(TransactionStatus? transactionStatus, bool isSubmitted)
        {
            if (transactionStatus is null)
                return isSubmitted ? "bg-info-100 text-info-600" : "bg-neutral-200 text-neutral-700";

            string code = NormalizeStatusCode(transactionStatus.Code);

            if (code is "REJECTED" or "WITHDRAWN")
                return "bg-danger-100 text-danger-600";

            if (code is "ACCEPTED" or "COMPLETED")
                return "bg-success-100 text-success-600";

            if (code is "REVIEWERASSIGNMENT" or "UNDERREVIEW" or "REVIEWSCOMPLETED" or "EDITORIALDECISION")
                return "bg-info-100 text-info-600";

            if (code is "REVISIONREQUESTED")
                return "bg-warning-100 text-warning-600";

            if (transactionStatus.IsFinal)
                return "bg-neutral-200 text-neutral-700";

            return isSubmitted ? "bg-warning-100 text-warning-600" : "bg-neutral-200 text-neutral-700";
        }

        private static string NormalizeStatusCode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return new string(value.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        }

        private static string GetAuthorFullName(Author author)
        {
            return string.Join(' ', new[] { author.FirstName, author.LastName }.Where(part => !string.IsNullOrWhiteSpace(part))).Trim();
        }

        private static string GetAuthorDisplayName(Author author, Guid requestedLanguageId, Guid defaultLanguageId)
        {
            string fullName = GetAuthorFullName(author);
            string? titleShortName = ResolveAuthorTitleShortName(author, requestedLanguageId, defaultLanguageId);

            return string.IsNullOrWhiteSpace(titleShortName)
                ? fullName
                : $"{titleShortName} {fullName}".Trim();
        }

        private static string? ResolveAuthorTitleShortName(Author author, Guid requestedLanguageId, Guid defaultLanguageId)
        {
            if (author.Title is null)
                return null;

            List<TitleTranslation> translations = author.Title.Translations
                .Where(translation => !IsDeleted(translation))
                .ToList();

            TitleTranslation? requestedTranslation = translations.FirstOrDefault(translation => translation.LanguageId == requestedLanguageId);
            if (!string.IsNullOrWhiteSpace(requestedTranslation?.Description))
                return requestedTranslation.Description.Trim();

            if (!string.IsNullOrWhiteSpace(requestedTranslation?.Name))
                return requestedTranslation.Name.Trim();

            TitleTranslation? defaultTranslation = translations.FirstOrDefault(translation => translation.LanguageId == defaultLanguageId);
            if (!string.IsNullOrWhiteSpace(defaultTranslation?.Description))
                return defaultTranslation.Description.Trim();

            if (!string.IsNullOrWhiteSpace(defaultTranslation?.Name))
                return defaultTranslation.Name.Trim();

            return NormalizeTitleCode(author.Title.Code);
        }

        private static string? NormalizeTitleCode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            return code.Trim().ToUpperInvariant() switch
            {
                "PROF_DR" => "Prof. Dr.",
                "ASSOC_PROF_DR" => "Doç. Dr.",
                "ASST_PROF_DR" => "Dr. Öğr. Üyesi",
                "DR" => "Dr.",
                "LECTURER" => "Öğr. Gör.",
                "RES_ASST" => "Arş. Gör.",
                _ => code.Replace('_', ' ').Trim()
            };
        }

        private static string? BuildOtherAuthorsText(List<Author> authors, Guid requestedLanguageId, Guid defaultLanguageId)
        {
            if (authors.Count == 0)
                return null;

            if (authors.Count == 1)
                return GetAuthorDisplayName(authors[0], requestedLanguageId, defaultLanguageId);

            return $"{GetAuthorDisplayName(authors[0], requestedLanguageId, defaultLanguageId)} • +{authors.Count - 1}";
        }

        private static bool Contains(string? value, string search)
        {
            return !string.IsNullOrWhiteSpace(value) && value.Contains(search, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDeleted(object entity)
        {
            object? deletedDate = entity.GetType().GetProperty("DeletedDate")?.GetValue(entity);
            return deletedDate is DateTime;
        }
    }
}
