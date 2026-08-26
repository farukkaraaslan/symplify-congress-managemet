using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Rules;

public class CongressSubmissionTypeBusinessRules : BaseBusinessRules
{
    private readonly ICongressRepository _congressRepository;
    private readonly ISubmissionTypeRepository _submissionTypeRepository;

    public CongressSubmissionTypeBusinessRules(
        ICongressRepository congressRepository,
        ISubmissionTypeRepository submissionTypeRepository)
    {
        _congressRepository = congressRepository;
        _submissionTypeRepository = submissionTypeRepository;
    }

    public async Task<Congress> CongressShouldExist(Guid congressId, CancellationToken cancellationToken)
    {
        if (congressId == Guid.Empty)
            throw new BusinessException(CongressSubmissionTypesMessages.CongressRequired);

        Congress? congress = await _congressRepository.GetAsync(
            predicate: entity => entity.Id == congressId,
            cancellationToken: cancellationToken);

        if (congress is null)
            throw new BusinessException(CongressSubmissionTypesMessages.CongressNotFound);

        return congress;
    }

    public Task CongressSubmissionTypeShouldExistWhenSelected(CongressSubmissionType? entity)
    {
        if (entity is null)
            throw new BusinessException(CongressSubmissionTypesMessages.EntityNotFound);

        return Task.CompletedTask;
    }

    public Task SelectionListShouldBeValid(IReadOnlyCollection<Guid> submissionTypeIds)
    {
        if (submissionTypeIds.Any(id => id == Guid.Empty))
            throw new BusinessException(CongressSubmissionTypesMessages.InvalidSelectionList);

        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<SubmissionType>> SubmissionTypesShouldExist(
        IReadOnlyCollection<Guid> submissionTypeIds,
        CancellationToken cancellationToken)
    {
        if (submissionTypeIds.Count == 0)
            return Array.Empty<SubmissionType>();

        List<SubmissionType> submissionTypes = _submissionTypeRepository
            .Query()
            .ToList()
            .Where(submissionType => submissionTypeIds.Contains(submissionType.Id) && !IsDeleted(submissionType))
            .ToList();

        HashSet<Guid> existingIds = submissionTypes.Select(submissionType => submissionType.Id).ToHashSet();

        if (submissionTypeIds.Any(id => !existingIds.Contains(id)))
            throw new BusinessException(CongressSubmissionTypesMessages.SubmissionTypeNotFound);

        return await Task.FromResult(submissionTypes);
    }

    private static bool IsDeleted(object entity)
    {
        object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
        return deletedDate is not null;
    }
}
