using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class CongressSliderTranslationRepository : EfRepositoryBase<CongressSliderTranslation, BackOfficeDbContext, Guid>, ICongressSliderTranslationRepository
{
    public CongressSliderTranslationRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
