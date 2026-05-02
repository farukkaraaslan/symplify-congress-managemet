using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.TransactionStatuses.Constants;
using Symplify.BackOffice.Application.Features.TransactionStatuses.Rules;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
using Symplify.BackOffice.Domain.Workflow;
namespace Symplify.BackOffice.Application.Features.TransactionStatuses.Commands.Update;
public class UpdateTransactionStatusCommand : IRequest<UpdatedTransactionStatusResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetTransactionStatuses";
    public string[] Roles => new[] { TransactionStatusesOperationClaims.Admin, TransactionStatusesOperationClaims.Write, TransactionStatusesOperationClaims.Update };
    public class UpdateTransactionStatusCommandHandler : IRequestHandler<UpdateTransactionStatusCommand, UpdatedTransactionStatusResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name", "Description" };
        private readonly ITransactionStatusRepository _repository; private readonly ITransactionStatusTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider; private readonly IMapper _mapper; private readonly TransactionStatusBusinessRules _rules;
        public UpdateTransactionStatusCommandHandler(ITransactionStatusRepository repository, ITransactionStatusTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider, IMapper mapper, TransactionStatusBusinessRules rules) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; _mapper = mapper; _rules = rules; }
        public async Task<UpdatedTransactionStatusResponse> Handle(UpdateTransactionStatusCommand request, CancellationToken cancellationToken)
        {
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);
            TransactionStatus? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.TransactionStatusShouldExistWhenSelected(entity);
            entity!.Code = request.Code;
            entity!.Order = request.Order;
            entity!.IsActive = request.IsActive;
            TransactionStatus updatedEntity = await _repository.UpdateAsync(entity!);
            IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            HashSet<Guid> activeLanguageIds = activeLanguages.Select(x => x.Id).ToHashSet();
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            List<TransactionStatusTranslation> existingTranslations = _translationRepository.Query().ToList().Where(x => EqualityComparer<int>.Default.Equals(x.TransactionStatusId, request.Id)).ToList();
            foreach (TranslationInputDto input in request.Translations)
            {
                if (!activeLanguageIds.Contains(input.LanguageId)) continue;
                bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);
                if (!isDefaultLanguage && !hasAnyValue) continue;
                TransactionStatusTranslation? existingTranslation = existingTranslations.FirstOrDefault(x => x.LanguageId == input.LanguageId);
                if (existingTranslation is null)
                {
                    TransactionStatusTranslation translation = new();
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "TransactionStatusId", request.Id);
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "LanguageId", input.LanguageId);
                    LocalizedEntityRuntimeHelper.ApplyFieldDictionary(translation, TranslationFieldNames, input.Fields);
                    await _translationRepository.AddAsync(translation);
                    continue;
                }
                LocalizedEntityRuntimeHelper.ApplyFieldDictionary(existingTranslation, TranslationFieldNames, input.Fields);
                await _translationRepository.UpdateAsync(existingTranslation);
            }
            return _mapper.Map<UpdatedTransactionStatusResponse>(updatedEntity);
        }
    }
}
