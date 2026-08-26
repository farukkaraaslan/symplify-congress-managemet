using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public sealed class ParticipationCertificateRepository
    : EfRepositoryBase<ParticipationCertificate, BackOfficeDbContext, Guid>,
      IParticipationCertificateRepository
{
    public ParticipationCertificateRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
