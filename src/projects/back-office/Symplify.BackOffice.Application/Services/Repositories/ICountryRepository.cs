using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Reference;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ICountryRepository : IAsyncRepository<Country, Guid>, IRepository<Country, Guid>
{
}
