using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Features.BulkEmails.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.BulkEmails.Rules;

public sealed class BulkEmailBusinessRules : BaseBusinessRules
{
    private readonly ICongressRepository _congressRepository;
    private readonly IOrganizationUserRepository _organizationUserRepository;

    public BulkEmailBusinessRules(
        ICongressRepository congressRepository,
        IOrganizationUserRepository organizationUserRepository)
    {
        _congressRepository = congressRepository;
        _organizationUserRepository = organizationUserRepository;
    }

    public async Task<Congress> GetAuthorizedCongressAsync(
        Guid congressId,
        Guid? currentUserId,
        bool isSuperAdmin,
        CancellationToken cancellationToken)
    {
        if (congressId == Guid.Empty)
            throw new BusinessException(BulkEmailsMessages.CongressRequired);

        // Toplu e-posta yalnızca yayındaki/aktif kongrelerde çalıştırılabilir.
        Congress? congress = await _congressRepository.GetAsync(
            predicate: item =>
                item.Id == congressId &&
                item.Status == CongressStatus.Published &&
                item.DeletedDate == null,
            cancellationToken: cancellationToken);

        if (congress is null)
            throw new BusinessException(BulkEmailsMessages.CongressNotFound);

        if (isSuperAdmin)
            return congress;

        if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
            throw new BusinessException(BulkEmailsMessages.CongressAccessDenied);

        bool hasMembership = await _organizationUserRepository
            .Query()
            .AsNoTracking()
            .AnyAsync(item =>
                item.UserId == currentUserId.Value &&
                item.OrganizationId == congress.OrganizationId &&
                item.IsActive &&
                item.DeletedDate == null,
                cancellationToken);

        if (!hasMembership)
            throw new BusinessException(BulkEmailsMessages.CongressAccessDenied);

        return congress;
    }
}
