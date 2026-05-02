using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class SubmissionEvaluationRepository : EfRepositoryBase<SubmissionEvaluation, BackOfficeDbContext, Guid>, ISubmissionEvaluationRepository
{
    public SubmissionEvaluationRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
