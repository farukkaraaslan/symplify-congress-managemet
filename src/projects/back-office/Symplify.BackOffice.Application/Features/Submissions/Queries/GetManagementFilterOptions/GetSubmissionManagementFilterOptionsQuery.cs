using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.Submissions.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Lookups;
using Symplify.BackOffice.Domain.Workflow;
using PaymentStatus = Symplify.BackOffice.Domain.Workflow.PaymentStatus;

namespace Symplify.BackOffice.Application.Features.Submissions.Queries.GetManagementFilterOptions;

public sealed class GetSubmissionManagementFilterOptionsQuery : IRequest<GetSubmissionManagementFilterOptionsResponse>, ISecuredRequest, ICachableRequest
{
    public string? Culture { get; set; }

    public bool ArchiveMode { get; set; }

    public string[] Roles => new[] { SubmissionsOperationClaims.Admin, SubmissionsOperationClaims.Read };

    public bool BypassCache => true;

    public string CacheKey => $"SubmissionManagementFilterOptions({Culture},{ArchiveMode})";

    public string CacheGroupKey => "GetSubmissions";

    public TimeSpan? SlidingExpiration { get; }

    public sealed class GetSubmissionManagementFilterOptionsQueryHandler : IRequestHandler<GetSubmissionManagementFilterOptionsQuery, GetSubmissionManagementFilterOptionsResponse>
    {
        private readonly ICongressRepository _congressRepository;
        private readonly ICongressTranslationRepository _congressTranslationRepository;
        private readonly ICongressTopicRepository _congressTopicRepository;
        private readonly ITopicTranslationRepository _topicTranslationRepository;
        private readonly ICongressSubmissionTypeRepository _congressSubmissionTypeRepository;
        private readonly ISubmissionTypeTranslationRepository _submissionTypeTranslationRepository;
        private readonly ITransactionStatusRepository _transactionStatusRepository;
        private readonly ITransactionStatusTranslationRepository _transactionStatusTranslationRepository;
        private readonly IPaymentStatusRepository _paymentStatusRepository;
        private readonly IPaymentStatusTranslationRepository _paymentStatusTranslationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetSubmissionManagementFilterOptionsQueryHandler(
            ICongressRepository congressRepository,
            ICongressTranslationRepository congressTranslationRepository,
            ICongressTopicRepository congressTopicRepository,
            ITopicTranslationRepository topicTranslationRepository,
            ICongressSubmissionTypeRepository congressSubmissionTypeRepository,
            ISubmissionTypeTranslationRepository submissionTypeTranslationRepository,
            ITransactionStatusRepository transactionStatusRepository,
            ITransactionStatusTranslationRepository transactionStatusTranslationRepository,
            IPaymentStatusRepository paymentStatusRepository,
            IPaymentStatusTranslationRepository paymentStatusTranslationRepository,
            IApplicationLanguageProvider languageProvider,
            ICurrentLanguageProvider currentLanguageProvider,
            ITranslationFallbackResolver fallbackResolver)
        {
            _congressRepository = congressRepository;
            _congressTranslationRepository = congressTranslationRepository;
            _congressTopicRepository = congressTopicRepository;
            _topicTranslationRepository = topicTranslationRepository;
            _congressSubmissionTypeRepository = congressSubmissionTypeRepository;
            _submissionTypeTranslationRepository = submissionTypeTranslationRepository;
            _transactionStatusRepository = transactionStatusRepository;
            _transactionStatusTranslationRepository = transactionStatusTranslationRepository;
            _paymentStatusRepository = paymentStatusRepository;
            _paymentStatusTranslationRepository = paymentStatusTranslationRepository;
            _languageProvider = languageProvider;
            _currentLanguageProvider = currentLanguageProvider;
            _fallbackResolver = fallbackResolver;
        }

        public async Task<GetSubmissionManagementFilterOptionsResponse> Handle(GetSubmissionManagementFilterOptionsQuery request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(request.Culture, defaultLanguage, cancellationToken);

            CongressStatus targetCongressStatus = request.ArchiveMode
                ? CongressStatus.Archived
                : CongressStatus.Published;

            List<Congress> visibleCongresses = _congressRepository
                .Query()
                .ToList()
                .Where(congress =>
                    !IsDeleted(congress) &&
                    congress.Status == targetCongressStatus)
                .OrderByDescending(congress => congress.StartDate)
                .ThenBy(congress => congress.Name)
                .ToList();

            List<CongressTranslation> congressTranslations = _congressTranslationRepository
                .Query()
                .ToList()
                .Where(translation => !IsDeleted(translation))
                .ToList();

            List<Domain.Workflow.TransactionStatus> transactionStatuses = _transactionStatusRepository
                .Query()
                .ToList()
                .Where(status => status.IsActive && !IsDeleted(status) && IsSubmissionManagementStatus(status.Code))
                .OrderBy(status => ResolveSubmissionManagementStatusOrder(status.Code))
                .ThenBy(status => status.Id)
                .ToList();

            List<TransactionStatusTranslation> transactionStatusTranslations = _transactionStatusTranslationRepository
                .Query()
                .ToList()
                .Where(translation => !IsDeleted(translation))
                .ToList();

            List<PaymentStatus> paymentStatuses = _paymentStatusRepository
                .Query()
                .ToList()
                .Where(status => status.IsActive && !IsDeleted(status))
                .OrderBy(status => status.Order <= 0 ? int.MaxValue : status.Order)
                .ThenBy(status => status.Id)
                .ToList();

            List<PaymentStatusTranslation> paymentStatusTranslations = _paymentStatusTranslationRepository
                .Query()
                .ToList()
                .Where(translation => !IsDeleted(translation))
                .ToList();

            HashSet<Guid> visibleCongressIds = visibleCongresses.Select(congress => congress.Id).ToHashSet();

            List<CongressTopic> topicRelations = _congressTopicRepository
                .Query()
                .ToList()
                .Where(relation => visibleCongressIds.Contains(relation.CongressId) && relation.IsActive && !IsDeleted(relation))
                .OrderBy(relation => relation.Order <= 0 ? int.MaxValue : relation.Order)
                .ThenBy(relation => relation.TopicId)
                .ToList();

            List<TopicTranslation> topicTranslations = _topicTranslationRepository
                .Query()
                .ToList()
                .Where(translation => !IsDeleted(translation))
                .ToList();

            List<CongressSubmissionType> submissionTypeRelations = _congressSubmissionTypeRepository
                .Query()
                .ToList()
                .Where(relation => visibleCongressIds.Contains(relation.CongressId) && relation.IsActive && !IsDeleted(relation))
                .OrderBy(relation => relation.Order <= 0 ? int.MaxValue : relation.Order)
                .ThenBy(relation => relation.SubmissionTypeId)
                .ToList();

            List<SubmissionTypeTranslation> submissionTypeTranslations = _submissionTypeTranslationRepository
                .Query()
                .ToList()
                .Where(translation => !IsDeleted(translation))
                .ToList();

            return new GetSubmissionManagementFilterOptionsResponse
            {
                Congresses = visibleCongresses.Select(congress => new SubmissionManagementFilterOptionDto
                    {
                        Value = congress.Id.ToString(),
                        Text = ResolveCongressName(congress, congressTranslations, requestedLanguage.Id, defaultLanguage.Id)
                    })
                    .ToList(),

                TransactionStatuses = transactionStatuses.Select(status => new SubmissionManagementFilterOptionDto
                    {
                        Value = status.Id.ToString(),
                        Text = ResolveName(
                            transactionStatusTranslations.Where(translation => translation.TransactionStatusId == status.Id),
                            requestedLanguage.Id,
                            defaultLanguage.Id) ?? status.Code
                    })
                    .ToList(),

                PaymentStatuses = paymentStatuses.Select(status => new SubmissionManagementFilterOptionDto
                    {
                        Value = status.Id.ToString(),
                        Text = ResolveName(
                            paymentStatusTranslations.Where(translation => translation.PaymentStatusId == status.Id),
                            requestedLanguage.Id,
                            defaultLanguage.Id) ?? status.Code
                    })
                    .ToList(),

                Topics = topicRelations
                    .GroupBy(relation => relation.TopicId)
                    .Select(group => new SubmissionManagementFilterOptionDto
                    {
                        Value = group.Key.ToString(),
                        Text = ResolveName(
                            topicTranslations.Where(translation => translation.TopicId == group.Key),
                            requestedLanguage.Id,
                            defaultLanguage.Id) ?? group.Key.ToString()
                    })
                    .OrderBy(item => item.Text)
                    .ToList(),

                SubmissionTypes = submissionTypeRelations
                    .GroupBy(relation => relation.SubmissionTypeId)
                    .Select(group => new SubmissionManagementFilterOptionDto
                    {
                        Value = group.Key.ToString(),
                        Text = ResolveName(
                            submissionTypeTranslations.Where(translation => translation.SubmissionTypeId == group.Key),
                            requestedLanguage.Id,
                            defaultLanguage.Id) ?? group.Key.ToString()
                    })
                    .OrderBy(item => item.Text)
                    .ToList()
            };
        }

        private async Task<ApplicationLanguageDto> ResolveRequestedLanguageAsync(string? culture, ApplicationLanguageDto defaultLanguage, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(culture))
                return await _languageProvider.GetByCultureAsync(culture, cancellationToken) ?? defaultLanguage;

            return await _currentLanguageProvider.GetCurrentLanguageAsync(cancellationToken);
        }

        private string ResolveCongressName(Congress congress, IEnumerable<CongressTranslation> translations, Guid requestedLanguageId, Guid defaultLanguageId)
        {
            CongressTranslation? translation = _fallbackResolver.Resolve(
                translations.Where(item => item.CongressId == congress.Id),
                requestedLanguageId,
                defaultLanguageId);

            string title = !string.IsNullOrWhiteSpace(translation?.Title)
                ? translation.Title
                : congress.Name;

            return string.IsNullOrWhiteSpace(congress.Code)
                ? title
                : $"{congress.Code} - {title}";
        }

        private string? ResolveName<TTranslation>(IEnumerable<TTranslation> translations, Guid requestedLanguageId, Guid defaultLanguageId)
            where TTranslation : class
        {
            TTranslation? displayTranslation = _fallbackResolver.Resolve(translations, requestedLanguageId, defaultLanguageId);
            object? value = displayTranslation?.GetType().GetProperty("Name")?.GetValue(displayTranslation);
            return value?.ToString();
        }

        private static bool IsSubmissionManagementStatus(string? code)
        {
            string normalized = NormalizeCode(code);
            return normalized is "SUBMITTED"
                or "REVIEWERASSIGNMENT"
                or "UNDERREVIEW"
                or "ACCEPTED"
                or "COMPLETED"
                or "REJECTED";
        }

        private static int ResolveSubmissionManagementStatusOrder(string? code)
        {
            return NormalizeCode(code) switch
            {
                "SUBMITTED" => 10,
                "REVIEWERASSIGNMENT" => 20,
                "UNDERREVIEW" => 30,
                "ACCEPTED" => 40,
                "COMPLETED" => 50,
                "REJECTED" => 60,
                _ => int.MaxValue
            };
        }

        private static string NormalizeCode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return new string(value.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        }

        private static bool IsDeleted(object entity)
        {
            object? deletedDate = entity.GetType().GetProperty("DeletedDate")?.GetValue(entity);
            return deletedDate is DateTime;
        }
    }
}
