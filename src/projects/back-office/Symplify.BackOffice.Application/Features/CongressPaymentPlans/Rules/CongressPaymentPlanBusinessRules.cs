using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressPaymentPlans.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressPaymentPlans.Rules;

public class CongressPaymentPlanBusinessRules : BaseBusinessRules
{
    private readonly IApplicationLanguageProvider _applicationLanguageProvider;
    private readonly ICongressPaymentPlanRepository _repository;

    public CongressPaymentPlanBusinessRules(
        IApplicationLanguageProvider applicationLanguageProvider,
        ICongressPaymentPlanRepository repository)
    {
        _applicationLanguageProvider = applicationLanguageProvider;
        _repository = repository;
    }

    public Task CongressPaymentPlanShouldExistWhenSelected(CongressPaymentPlan? entity)
    {
        if (entity is null)
            throw new BusinessException(CongressPaymentPlansMessages.EntityNotFound);

        return Task.CompletedTask;
    }

    public async Task DefaultTranslationShouldExist(
        IEnumerable<TranslationInputDto> translations,
        CancellationToken cancellationToken)
    {
        ApplicationLanguageDto defaultLanguage = await _applicationLanguageProvider.GetDefaultLanguageAsync(cancellationToken);
        TranslationInputDto? defaultTranslation = translations.FirstOrDefault(x => x.LanguageId == defaultLanguage.Id);

        if (defaultTranslation is null || !LocalizedEntityRuntimeHelper.HasRequiredField(defaultTranslation.Fields, "Name"))
            throw new BusinessException(CongressPaymentPlansMessages.DefaultTranslationRequired);
    }

    public Task AudienceTypeShouldBeValid(string? audienceType)
    {
        if (!CongressPaymentPlanAudienceTypes.IsValid(audienceType))
            throw new BusinessException(CongressPaymentPlansMessages.InvalidAudienceType);

        return Task.CompletedTask;
    }

    public Task PaymentCategoryShouldBeValid(string? paymentCategory)
    {
        if (!CongressPaymentPlanCategories.IsValid(paymentCategory))
            throw new BusinessException(CongressPaymentPlansMessages.InvalidPaymentCategory);

        return Task.CompletedTask;
    }

    public Task DateRangeShouldBeValid(DateTime? validFrom, DateTime? validUntil)
    {
        if (validFrom.HasValue && validUntil.HasValue && validUntil.Value < validFrom.Value)
            throw new BusinessException(CongressPaymentPlansMessages.InvalidDateRange);

        return Task.CompletedTask;
    }

    public Task CodeShouldBeUniqueInCongress(Guid congressId, string code, Guid? ignoredId = null)
    {
        bool exists = _repository.Query()
            .ToList()
            .Any(entity =>
                entity.CongressId == congressId &&
                string.Equals(entity.Code, code, StringComparison.OrdinalIgnoreCase) &&
                (!ignoredId.HasValue || entity.Id != ignoredId.Value) &&
                !IsDeleted(entity));

        if (exists)
            throw new BusinessException(CongressPaymentPlansMessages.CodeAlreadyExists);

        return Task.CompletedTask;
    }

    private static bool IsDeleted(object entity)
    {
        object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
        return deletedDate is not null;
    }
}
