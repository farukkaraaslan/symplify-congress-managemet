using Symplify.BackOffice.Application.Features.ProgramManagement.Models;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.ProgramManagement.Services;

public interface ICongressProgramScheduler
{
    (CongressProgramPlan Plan, ProgramGenerationResult Result) CreatePlan(
        ProgramGenerationSourceDto source,
        ProgramGenerationSettings settings);

    ProgramGenerationResult FillUnassigned(
        CongressProgramPlan plan,
        ProgramGenerationSourceDto source,
        ProgramGenerationSettings settings);
}
