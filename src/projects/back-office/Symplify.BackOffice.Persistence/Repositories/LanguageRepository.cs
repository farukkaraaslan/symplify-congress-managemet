using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Localization;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class LanguageRepository : EfRepositoryBase<Language, BackOfficeDbContext, Guid>, ILanguageRepository
{
    public LanguageRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
