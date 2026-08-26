using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressPaymentPlans.Constants;
using Symplify.BackOffice.Application.Features.CongressPaymentPlans.Rules;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressPaymentPlans.Commands.Create;

public class CreateCongressPaymentPlanCommand : IRequest<CreatedCongressPaymentPlanResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressId { get; set; }
    public string? Code { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string AudienceType { get; set; } = CongressPaymentPlanAudienceTypes.All;
    public string PaymentCategory { get; set; } = CongressPaymentPlanCategories.Participation;
    public DateTime? DueDate { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }

    /// <summary>
    /// Geriye uyumluluk için tutulur; create sırasında kullanıcıdan alınmaz, handler Max(Order)+1 üretir.
    /// </summary>
    public int Order { get; set; }

    public bool IsPublicVisible { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressPaymentPlans";
    public string[] Roles => new[] { CongressPaymentPlansOperationClaims.Admin, CongressPaymentPlansOperationClaims.Write, CongressPaymentPlansOperationClaims.Add };

    public class CreateCongressPaymentPlanCommandHandler : IRequestHandler<CreateCongressPaymentPlanCommand, CreatedCongressPaymentPlanResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name", "Description" };
        private readonly ICongressPaymentPlanRepository _repository;
        private readonly ICongressPaymentPlanTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly IMapper _mapper;
        private readonly CongressPaymentPlanBusinessRules _rules;

        public CreateCongressPaymentPlanCommandHandler(
            ICongressPaymentPlanRepository repository,
            ICongressPaymentPlanTranslationRepository translationRepository,
            IApplicationLanguageProvider languageProvider,
            IMapper mapper,
            CongressPaymentPlanBusinessRules rules)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<CreatedCongressPaymentPlanResponse> Handle(CreateCongressPaymentPlanCommand request, CancellationToken cancellationToken)
        {
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);
            await _rules.AudienceTypeShouldBeValid(request.AudienceType);
            await _rules.PaymentCategoryShouldBeValid(request.PaymentCategory);
            await _rules.DateRangeShouldBeValid(request.ValidFrom, request.ValidUntil);

            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            string defaultName = GetDefaultTranslationName(request.Translations, defaultLanguage.Id);
            string normalizedAudienceType = CongressPaymentPlanAudienceTypes.Normalize(request.AudienceType);
            string normalizedPaymentCategory = CongressPaymentPlanCategories.Normalize(request.PaymentCategory);
            string normalizedCurrency = NormalizeCurrency(request.Currency);
            string normalizedCode = NormalizeCode(request.Code, defaultName, normalizedCurrency, normalizedAudienceType, normalizedPaymentCategory);

            await _rules.CodeShouldBeUniqueInCongress(request.CongressId, normalizedCode);

            CongressPaymentPlan entity = new()
            {
                Id = Guid.NewGuid(),
                CongressId = request.CongressId,
                Code = normalizedCode,
                Amount = request.Amount,
                Currency = normalizedCurrency,
                AudienceType = normalizedAudienceType,
                PaymentCategory = normalizedPaymentCategory,
                DueDate = request.DueDate,
                ValidFrom = request.ValidFrom,
                ValidUntil = request.ValidUntil,
                Order = GetNextOrder(request.CongressId),
                IsPublicVisible = request.IsPublicVisible,
                IsActive = request.IsActive
            };

            CongressPaymentPlan createdEntity = await _repository.AddAsync(entity);

            IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            HashSet<Guid> activeLanguageIds = activeLanguages.Select(x => x.Id).ToHashSet();

            foreach (TranslationInputDto input in request.Translations)
            {
                if (!activeLanguageIds.Contains(input.LanguageId))
                    continue;

                bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);

                if (!isDefaultLanguage && !hasAnyValue)
                    continue;

                CongressPaymentPlanTranslation translation = new();
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "CongressPaymentPlanId", createdEntity.Id);
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "LanguageId", input.LanguageId);
                LocalizedEntityRuntimeHelper.ApplyFieldDictionary(translation, TranslationFieldNames, input.Fields);

                await _translationRepository.AddAsync(translation);
            }

            return _mapper.Map<CreatedCongressPaymentPlanResponse>(createdEntity);
        }

        private int GetNextOrder(Guid congressId)
        {
            List<CongressPaymentPlan> entities = _repository.Query()
                .ToList()
                .Where(entity => entity.CongressId == congressId && !IsDeleted(entity))
                .ToList();

            return entities.Count == 0
                ? 1
                : entities.Max(entity => entity.Order <= 0 ? 0 : entity.Order) + 1;
        }

        private static string GetDefaultTranslationName(IEnumerable<TranslationInputDto> translations, Guid defaultLanguageId)
        {
            TranslationInputDto? defaultTranslation = translations.FirstOrDefault(translation => translation.LanguageId == defaultLanguageId);

            if (defaultTranslation is not null && defaultTranslation.Fields.TryGetValue("Name", out string? name) && !string.IsNullOrWhiteSpace(name))
                return name;

            return "payment-plan";
        }

        private static string NormalizeCurrency(string? currency)
            => string.IsNullOrWhiteSpace(currency)
                ? "TRY"
                : currency.Trim().ToUpperInvariant();

        private static string NormalizeCode(
            string? code,
            string defaultName,
            string currency,
            string audienceType,
            string paymentCategory)
        {
            string source = !string.IsNullOrWhiteSpace(code)
                ? code
                : $"{audienceType}-{paymentCategory}-{currency}-{defaultName}";

            string normalized = ToAsciiSlug(source).Replace('-', '_').ToUpperInvariant();

            return normalized.Length <= 128
                ? normalized
                : normalized[..128].Trim('_');
        }

        private static string ToAsciiSlug(string value)
        {
            string normalizedValue = value.Trim().Normalize(NormalizationForm.FormD);
            StringBuilder builder = new();

            foreach (char character in normalizedValue)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);

                if (category != UnicodeCategory.NonSpacingMark)
                    builder.Append(character);
            }

            string ascii = builder.ToString()
                .Normalize(NormalizationForm.FormC)
                .Replace('ı', 'i')
                .Replace('İ', 'I');

            string slug = Regex.Replace(ascii.ToLowerInvariant(), "[^a-z0-9]+", "-");
            slug = Regex.Replace(slug, "-{2,}", "-").Trim('-');

            return string.IsNullOrWhiteSpace(slug) ? "payment-plan" : slug;
        }

        private static bool IsDeleted(object entity)
        {
            object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
            return deletedDate is not null;
        }
    }
}
