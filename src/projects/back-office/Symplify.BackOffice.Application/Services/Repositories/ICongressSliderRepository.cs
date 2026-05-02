using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ICongressSliderRepository : IAsyncRepository<CongressSlider, Guid>, IRepository<CongressSlider, Guid>
{
}
