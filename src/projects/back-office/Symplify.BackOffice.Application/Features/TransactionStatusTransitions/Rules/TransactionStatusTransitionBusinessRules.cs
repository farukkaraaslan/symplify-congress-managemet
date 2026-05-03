using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Rules;

public class TransactionStatusTransitionBusinessRules : BaseBusinessRules
{
    private readonly IApplicationLanguageProvider _applicationLanguageProvider;
    private readonly ITransactionStatusRepository _transactionStatusRepository;
    private readonly ITransactionStatusTransitionRepository _transactionStatusTransitionRepository;

    public TransactionStatusTransitionBusinessRules(
        IApplicationLanguageProvider applicationLanguageProvider,
        ITransactionStatusRepository transactionStatusRepository,
        ITransactionStatusTransitionRepository transactionStatusTransitionRepository)
    {
        _applicationLanguageProvider = applicationLanguageProvider;
        _transactionStatusRepository = transactionStatusRepository;
        _transactionStatusTransitionRepository = transactionStatusTransitionRepository;
    }

    public Task TransactionStatusTransitionShouldExistWhenSelected(TransactionStatusTransition? entity)
    {
        if (entity is null)
            throw new BusinessException(TransactionStatusTransitionsMessages.EntityNotFound);

        return Task.CompletedTask;
    }

    public async Task DefaultTranslationShouldExist(
        IEnumerable<TranslationInputDto> translations,
        CancellationToken cancellationToken)
    {
        ApplicationLanguageDto defaultLanguage = await _applicationLanguageProvider.GetDefaultLanguageAsync(cancellationToken);

        TranslationInputDto? defaultTranslation = translations.FirstOrDefault(translation => translation.LanguageId == defaultLanguage.Id);

        if (defaultTranslation is null || !LocalizedEntityRuntimeHelper.HasRequiredField(defaultTranslation.Fields, "Name"))
            throw new BusinessException(TransactionStatusTransitionsMessages.DefaultTranslationRequired);
    }

    public async Task StatusesShouldExistAndBeUsable(int fromStatusId, int toStatusId)
    {
        TransactionStatus? fromStatus = await _transactionStatusRepository.GetAsync(
            predicate: status => status.Id.Equals(fromStatusId));

        if (fromStatus is null)
            throw new BusinessException(TransactionStatusTransitionsMessages.FromStatusNotFound);

        if (!fromStatus.IsActive)
            throw new BusinessException(TransactionStatusTransitionsMessages.FromStatusPassive);

        if (fromStatus.IsFinal)
            throw new BusinessException(TransactionStatusTransitionsMessages.FromStatusCannotBeFinal);

        TransactionStatus? toStatus = await _transactionStatusRepository.GetAsync(
            predicate: status => status.Id.Equals(toStatusId));

        if (toStatus is null)
            throw new BusinessException(TransactionStatusTransitionsMessages.ToStatusNotFound);

        if (!toStatus.IsActive)
            throw new BusinessException(TransactionStatusTransitionsMessages.ToStatusPassive);
    }

    public Task FromAndToStatusShouldBeDifferent(int fromStatusId, int toStatusId)
    {
        if (fromStatusId == toStatusId)
            throw new BusinessException(TransactionStatusTransitionsMessages.FromAndToStatusCannotBeSame);

        return Task.CompletedTask;
    }

    public Task TransitionShouldBeUniqueWhenCreating(int fromStatusId, int toStatusId)
    {
        bool exists = _transactionStatusTransitionRepository.Query()
            .ToList()
            .Where(transition => !IsDeleted(transition))
            .Any(transition => transition.FromStatusId == fromStatusId && transition.ToStatusId == toStatusId);

        if (exists)
            throw new BusinessException(TransactionStatusTransitionsMessages.TransitionAlreadyExists);

        return Task.CompletedTask;
    }

    public Task TransitionShouldBeUniqueWhenUpdating(int id, int fromStatusId, int toStatusId)
    {
        bool exists = _transactionStatusTransitionRepository.Query()
            .ToList()
            .Where(transition => !IsDeleted(transition))
            .Any(transition =>
                transition.Id != id &&
                transition.FromStatusId == fromStatusId &&
                transition.ToStatusId == toStatusId);

        if (exists)
            throw new BusinessException(TransactionStatusTransitionsMessages.TransitionAlreadyExists);

        return Task.CompletedTask;
    }

    private static bool IsDeleted(object entity)
    {
        object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
        return deletedDate is not null;
    }
}
