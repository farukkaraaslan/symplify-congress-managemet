using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class CongressEvaluationCriterionRepository : EfRepositoryBase<CongressEvaluationCriterion, BackOfficeDbContext, Guid>, ICongressEvaluationCriterionRepository
{
    public CongressEvaluationCriterionRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
