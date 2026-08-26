using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ICongressSliderTranslationRepository : IAsyncRepository<CongressSliderTranslation, Guid>, IRepository<CongressSliderTranslation, Guid>
{
}
