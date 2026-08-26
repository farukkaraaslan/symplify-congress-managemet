using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public sealed class CongressContactEmailRepository :
    EfRepositoryBase<CongressContactEmail, BackOfficeDbContext, Guid>,
    ICongressContactEmailRepository
{
    public CongressContactEmailRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
