using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class AuthorRepository : EfRepositoryBase<Author, BackOfficeDbContext, Guid>, IAuthorRepository
{
    public AuthorRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
