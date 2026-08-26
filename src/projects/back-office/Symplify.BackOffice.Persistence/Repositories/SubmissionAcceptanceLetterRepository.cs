using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public sealed class SubmissionAcceptanceLetterRepository : EfRepositoryBase<SubmissionAcceptanceLetter, BackOfficeDbContext, Guid>, ISubmissionAcceptanceLetterRepository
{
    public SubmissionAcceptanceLetterRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
