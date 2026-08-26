using Symplify.BackOffice.Application.Features.ProgramManagement.Models;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface IProgramManagementRepository
{
    Task<IReadOnlyList<ProgramCongressOptionDto>> GetCongressOptionsAsync(string? culture, CancellationToken cancellationToken);
    Task<ProgramGenerationSourceDto?> GetGenerationSourceAsync(Guid congressId, IReadOnlyCollection<Guid>? roomIds, string? culture, CancellationToken cancellationToken, ProgramSubmissionFilterDto? filter = null);
    Task<CongressProgramPlan?> GetPlanForDisplayAsync(Guid congressId, CancellationToken cancellationToken);
    Task<CongressProgramPlan?> GetPlanForUpdateAsync(Guid congressId, CancellationToken cancellationToken);
    Task<bool> AreAuthorsEligibleForCongressAsync(Guid congressId, IReadOnlyCollection<Guid> authorIds, CancellationToken cancellationToken);
    Task<bool> AreBoardMembersEligibleForCongressAsync(Guid congressId, IReadOnlyCollection<Guid> boardMemberIds, CancellationToken cancellationToken);
    Task AddPlanAsync(CongressProgramPlan plan, CancellationToken cancellationToken);
    void RemovePlan(CongressProgramPlan plan);
    void RemoveFixedBlock(CongressProgramFixedBlock fixedBlock);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
