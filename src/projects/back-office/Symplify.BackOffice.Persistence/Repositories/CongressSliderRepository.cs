using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class CongressSliderRepository : EfRepositoryBase<CongressSlider, BackOfficeDbContext, Guid>, ICongressSliderRepository
{
    public CongressSliderRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
