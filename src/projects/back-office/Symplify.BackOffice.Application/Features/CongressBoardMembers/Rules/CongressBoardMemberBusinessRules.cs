using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Constants;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Rules;

public class CongressBoardMemberBusinessRules : BaseBusinessRules
{
    private const long MaxImageSizeInBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    public Task CongressBoardMemberShouldExistWhenSelected(CongressBoardMember? entity)
    {
        if (entity is null)
            throw new BusinessException(CongressBoardMembersMessages.EntityNotFound);

        return Task.CompletedTask;
    }

    public Task CongressShouldBeSelected(Guid congressId)
    {
        if (congressId == Guid.Empty)
            throw new BusinessException(CongressBoardMembersMessages.CongressRequired);

        return Task.CompletedTask;
    }

    public Task FullNameShouldNotBeEmpty(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new BusinessException(CongressBoardMembersMessages.FullNameRequired);

        return Task.CompletedTask;
    }

    public Task ImageShouldBeValid(CongressBoardMemberImageInputDto? image)
    {
        if (image is null || image.Length <= 0)
            return Task.CompletedTask;

        string extension = Path.GetExtension(image.OriginalFileName);

        if (!AllowedImageExtensions.Contains(extension))
            throw new BusinessException(CongressBoardMembersMessages.ImageExtensionInvalid);

        if (image.Length > MaxImageSizeInBytes)
            throw new BusinessException(CongressBoardMembersMessages.ImageSizeInvalid);

        return Task.CompletedTask;
    }
}
