using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class EvaluationCriterionRepository : EfRepositoryBase<EvaluationCriterion, BackOfficeDbContext, Guid>, IEvaluationCriterionRepository
{
    public EvaluationCriterionRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
