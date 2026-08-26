using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Reference.Translations;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface IStateTranslationRepository : IAsyncRepository<StateTranslation, Guid>, IRepository<StateTranslation, Guid>
{
}
