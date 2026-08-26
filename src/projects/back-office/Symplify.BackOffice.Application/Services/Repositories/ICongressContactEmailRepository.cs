using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface ICongressContactEmailRepository :
    IAsyncRepository<CongressContactEmail, Guid>,
    IRepository<CongressContactEmail, Guid>
{
}
