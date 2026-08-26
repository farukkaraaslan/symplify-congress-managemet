using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.DocumentTypes.Constants;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Domain.Lookups;
namespace Symplify.BackOffice.Application.Features.DocumentTypes.Rules;
public class DocumentTypeBusinessRules : BaseBusinessRules
{
    private readonly IApplicationLanguageProvider _applicationLanguageProvider;
    public DocumentTypeBusinessRules(IApplicationLanguageProvider applicationLanguageProvider) => _applicationLanguageProvider = applicationLanguageProvider;
    public Task DocumentTypeShouldExistWhenSelected(DocumentType? entity) { if (entity is null) throw new BusinessException(DocumentTypesMessages.EntityNotFound); return Task.CompletedTask; }
    public async Task DefaultTranslationShouldExist(IEnumerable<TranslationInputDto> translations, CancellationToken cancellationToken)
    {
        ApplicationLanguageDto defaultLanguage = await _applicationLanguageProvider.GetDefaultLanguageAsync(cancellationToken);
        TranslationInputDto? defaultTranslation = translations.FirstOrDefault(x => x.LanguageId == defaultLanguage.Id);
        if (defaultTranslation is null || !LocalizedEntityRuntimeHelper.HasRequiredField(defaultTranslation.Fields, "Name"))
            throw new BusinessException(DocumentTypesMessages.DefaultTranslationRequired);
    }
}
