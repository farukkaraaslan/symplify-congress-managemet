using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class EvaluationScoreRepository : EfRepositoryBase<EvaluationScore, BackOfficeDbContext, Guid>, IEvaluationScoreRepository
{
    public EvaluationScoreRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
