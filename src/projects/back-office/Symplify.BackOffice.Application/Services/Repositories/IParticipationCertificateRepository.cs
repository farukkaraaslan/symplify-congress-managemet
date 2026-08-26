using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface IParticipationCertificateRepository
    : IAsyncRepository<ParticipationCertificate, Guid>,
      IRepository<ParticipationCertificate, Guid>
{
}
