using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class EvaluationScoreOptionRepository : EfRepositoryBase<EvaluationScoreOption, BackOfficeDbContext, Guid>, IEvaluationScoreOptionRepository
{
    public EvaluationScoreOptionRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
