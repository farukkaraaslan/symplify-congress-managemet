using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.TransactionStatuses.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.TransactionStatuses.Rules;

public class TransactionStatusBusinessRules : BaseBusinessRules
{
    private readonly IApplicationLanguageProvider _applicationLanguageProvider;
    private readonly ITransactionStatusPhaseRepository _transactionStatusPhaseRepository;
    private readonly ITransactionStatusRepository _transactionStatusRepository;
    private readonly ITransactionStatusTransitionRepository _transactionStatusTransitionRepository;
    private readonly ISubmissionRepository _submissionRepository;
    private readonly ISubmissionHistoryRepository _submissionHistoryRepository;

    public TransactionStatusBusinessRules(
        IApplicationLanguageProvider applicationLanguageProvider,
        ITransactionStatusPhaseRepository transactionStatusPhaseRepository,
        ITransactionStatusRepository transactionStatusRepository,
        ITransactionStatusTransitionRepository transactionStatusTransitionRepository,
        ISubmissionRepository submissionRepository,
        ISubmissionHistoryRepository submissionHistoryRepository)
    {
        _applicationLanguageProvider = applicationLanguageProvider;
        _transactionStatusPhaseRepository = transactionStatusPhaseRepository;
        _transactionStatusRepository = transactionStatusRepository;
        _transactionStatusTransitionRepository = transactionStatusTransitionRepository;
        _submissionRepository = submissionRepository;
        _submissionHistoryRepository = submissionHistoryRepository;
    }

    public Task TransactionStatusShouldExistWhenSelected(TransactionStatus? entity)
    {
        if (entity is null)
            throw new BusinessException(TransactionStatusesMessages.EntityNotFound);

        return Task.CompletedTask;
    }

    public async Task TransactionStatusPhaseShouldExistAndBeActive(int phaseId)
    {
        TransactionStatusPhase? phase = await _transactionStatusPhaseRepository.GetAsync(
            predicate: entity => entity.Id.Equals(phaseId));

        if (phase is null)
            throw new BusinessException(TransactionStatusesMessages.PhaseNotFound);

        if (!phase.IsActive)
            throw new BusinessException(TransactionStatusesMessages.PhasePassive);
    }

    public async Task DefaultTranslationShouldExist(
        IEnumerable<TranslationInputDto> translations,
        CancellationToken cancellationToken)
    {
        ApplicationLanguageDto defaultLanguage = await _applicationLanguageProvider.GetDefaultLanguageAsync(cancellationToken);

        TranslationInputDto? defaultTranslation = translations.FirstOrDefault(translation => translation.LanguageId == defaultLanguage.Id);

        if (defaultTranslation is null || !LocalizedEntityRuntimeHelper.HasRequiredField(defaultTranslation.Fields, "Name"))
            throw new BusinessException(TransactionStatusesMessages.DefaultTranslationRequired);
    }

    public Task CodeShouldBeUniqueWhenCreating(string? code)
    {
        string normalizedCode = NormalizeCode(code);

        bool exists = _transactionStatusRepository.Query()
            .ToList()
            .Where(entity => !IsDeleted(entity))
            .Any(entity => string.Equals(NormalizeCode(entity.Code), normalizedCode, StringComparison.Ordinal));

        if (exists)
            throw new BusinessException(TransactionStatusesMessages.CodeAlreadyExists);

        return Task.CompletedTask;
    }

    public Task CodeShouldBeUniqueWhenUpdating(int id, string? code)
    {
        string normalizedCode = NormalizeCode(code);

        bool exists = _transactionStatusRepository.Query()
            .ToList()
            .Where(entity => !IsDeleted(entity))
            .Any(entity => entity.Id != id && string.Equals(NormalizeCode(entity.Code), normalizedCode, StringComparison.Ordinal));

        if (exists)
            throw new BusinessException(TransactionStatusesMessages.CodeAlreadyExists);

        return Task.CompletedTask;
    }

    public Task FinalStatusShouldNotHaveOutgoingTransition(int id, bool isFinal)
    {
        if (!isFinal)
            return Task.CompletedTask;

        bool hasOutgoingTransition = _transactionStatusTransitionRepository.Query()
            .ToList()
            .Where(transition => !IsDeleted(transition))
            .Any(transition => transition.FromStatusId == id && transition.IsActive);

        if (hasOutgoingTransition)
            throw new BusinessException(TransactionStatusesMessages.FinalStatusCannotHaveOutgoingTransition);

        return Task.CompletedTask;
    }

    public Task TransactionStatusShouldNotBeUsedByTransition(int id)
    {
        bool isUsed = _transactionStatusTransitionRepository.Query()
            .ToList()
            .Where(transition => !IsDeleted(transition))
            .Any(transition => transition.FromStatusId == id || transition.ToStatusId == id);

        if (isUsed)
            throw new BusinessException(TransactionStatusesMessages.EntityUsedByTransition);

        return Task.CompletedTask;
    }

    public Task TransactionStatusShouldNotBeUsedBySubmission(int id)
    {
        bool isUsed = _submissionRepository.Query()
            .ToList()
            .Where(submission => !IsDeleted(submission))
            .Any(submission => submission.TransactionStatusId == id);

        if (isUsed)
            throw new BusinessException(TransactionStatusesMessages.EntityUsedBySubmission);

        return Task.CompletedTask;
    }

    public Task TransactionStatusShouldNotBeUsedBySubmissionHistory(int id)
    {
        bool isUsed = _submissionHistoryRepository.Query()
            .ToList()
            .Where(history => !IsDeleted(history))
            .Any(history => history.FromStatusId == id || history.ToStatusId == id);

        if (isUsed)
            throw new BusinessException(TransactionStatusesMessages.EntityUsedBySubmissionHistory);

        return Task.CompletedTask;
    }

    private static string NormalizeCode(string? code)
    {
        return (code ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static bool IsDeleted(object entity)
    {
        object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
        return deletedDate is not null;
    }
}
