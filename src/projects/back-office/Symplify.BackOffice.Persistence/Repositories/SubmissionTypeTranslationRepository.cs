using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class SubmissionTypeTranslationRepository : EfRepositoryBase<SubmissionTypeTranslation, BackOfficeDbContext, Guid>, ISubmissionTypeTranslationRepository
{
    public SubmissionTypeTranslationRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
