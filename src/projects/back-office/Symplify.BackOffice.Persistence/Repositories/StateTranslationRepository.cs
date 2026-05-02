using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Reference.Translations;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class StateTranslationRepository : EfRepositoryBase<StateTranslation, BackOfficeDbContext, Guid>, IStateTranslationRepository
{
    public StateTranslationRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
