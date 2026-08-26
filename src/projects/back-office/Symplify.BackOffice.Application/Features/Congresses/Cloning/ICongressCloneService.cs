namespace Symplify.BackOffice.Application.Features.Congresses.Cloning;

public interface ICongressCloneService
{
    Task<CongressCloneResult> CloneAsync(
        CongressCloneRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteCreatedCongressAsync(
        Guid congressId,
        CancellationToken cancellationToken = default);
}
