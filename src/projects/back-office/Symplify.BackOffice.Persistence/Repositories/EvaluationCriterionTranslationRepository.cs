using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class EvaluationCriterionTranslationRepository : EfRepositoryBase<EvaluationCriterionTranslation, BackOfficeDbContext, Guid>, IEvaluationCriterionTranslationRepository
{
    public EvaluationCriterionTranslationRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
