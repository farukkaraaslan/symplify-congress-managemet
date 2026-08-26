using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.PaymentStatuses.Constants;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Domain.Workflow;
namespace Symplify.BackOffice.Application.Features.PaymentStatuses.Rules;
public class PaymentStatusBusinessRules : BaseBusinessRules
{
    private readonly IApplicationLanguageProvider _applicationLanguageProvider;
    public PaymentStatusBusinessRules(IApplicationLanguageProvider applicationLanguageProvider) => _applicationLanguageProvider = applicationLanguageProvider;
    public Task PaymentStatusShouldExistWhenSelected(PaymentStatus? entity) { if (entity is null) throw new BusinessException(PaymentStatusesMessages.EntityNotFound); return Task.CompletedTask; }
    public async Task DefaultTranslationShouldExist(IEnumerable<TranslationInputDto> translations, CancellationToken cancellationToken)
    {
        ApplicationLanguageDto defaultLanguage = await _applicationLanguageProvider.GetDefaultLanguageAsync(cancellationToken);
        TranslationInputDto? defaultTranslation = translations.FirstOrDefault(x => x.LanguageId == defaultLanguage.Id);
        if (defaultTranslation is null || !LocalizedEntityRuntimeHelper.HasRequiredField(defaultTranslation.Fields, "Name"))
            throw new BusinessException(PaymentStatusesMessages.DefaultTranslationRequired);
    }
}
