using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Constants;
using Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Rules;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
using Symplify.BackOffice.Domain.Workflow;
namespace Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Commands.Update;
public class UpdateTransactionStatusTransitionCommand : IRequest<UpdatedTransactionStatusTransitionResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public int Id { get; set; }
    public int FromStatusId { get; set; }
    public int ToStatusId { get; set; }
    public bool IsActive { get; set; }
    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetTransactionStatusTransitions";
    public string[] Roles => new[] { TransactionStatusTransitionsOperationClaims.Admin, TransactionStatusTransitionsOperationClaims.Write, TransactionStatusTransitionsOperationClaims.Update };
    public class UpdateTransactionStatusTransitionCommandHandler : IRequestHandler<UpdateTransactionStatusTransitionCommand, UpdatedTransactionStatusTransitionResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name", "Description" };
        private readonly ITransactionStatusTransitionRepository _repository; private readonly ITransactionStatusTransitionTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider; private readonly IMapper _mapper; private readonly TransactionStatusTransitionBusinessRules _rules;
        public UpdateTransactionStatusTransitionCommandHandler(ITransactionStatusTransitionRepository repository, ITransactionStatusTransitionTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider, IMapper mapper, TransactionStatusTransitionBusinessRules rules) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; _mapper = mapper; _rules = rules; }
        public async Task<UpdatedTransactionStatusTransitionResponse> Handle(UpdateTransactionStatusTransitionCommand request, CancellationToken cancellationToken)
        {
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);
            TransactionStatusTransition? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.TransactionStatusTransitionShouldExistWhenSelected(entity);
            entity!.FromStatusId = request.FromStatusId;
            entity!.ToStatusId = request.ToStatusId;
            entity!.IsActive = request.IsActive;
            TransactionStatusTransition updatedEntity = await _repository.UpdateAsync(entity!);
            IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            HashSet<Guid> activeLanguageIds = activeLanguages.Select(x => x.Id).ToHashSet();
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            List<TransactionStatusTransitionTranslation> existingTranslations = _translationRepository.Query().ToList().Where(x => EqualityComparer<int>.Default.Equals(x.TransactionStatusTransitionId, request.Id)).ToList();
            foreach (TranslationInputDto input in request.Translations)
            {
                if (!activeLanguageIds.Contains(input.LanguageId)) continue;
                bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);
                if (!isDefaultLanguage && !hasAnyValue) continue;
                TransactionStatusTransitionTranslation? existingTranslation = existingTranslations.FirstOrDefault(x => x.LanguageId == input.LanguageId);
                if (existingTranslation is null)
                {
                    TransactionStatusTransitionTranslation translation = new();
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "TransactionStatusTransitionId", request.Id);
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "LanguageId", input.LanguageId);
                    LocalizedEntityRuntimeHelper.ApplyFieldDictionary(translation, TranslationFieldNames, input.Fields);
                    await _translationRepository.AddAsync(translation);
                    continue;
                }
                LocalizedEntityRuntimeHelper.ApplyFieldDictionary(existingTranslation, TranslationFieldNames, input.Fields);
                await _translationRepository.UpdateAsync(existingTranslation);
            }
            return _mapper.Map<UpdatedTransactionStatusTransitionResponse>(updatedEntity);
        }
    }
}
